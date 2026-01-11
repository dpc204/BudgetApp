using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Budget.Api.Services;
using Budget.DB;
using Budget.Shared.Services;
using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Utilities.ImportExport;

/// <summary>
/// Exports all database tables to CSV format and stores them in Azure Blob Storage
/// </summary>
public static class ExportAll
{
  public sealed record Command : IRequest<Response>;

  public sealed record Response(string BackupId, string Message);

  /// <summary>
  /// Handles full database export to Azure Storage
  /// </summary>
  public class Handler(
    IServiceScopeFactory serviceScopeFactory,
    BlobServiceClient blobServiceClient,
    TableServiceClient tableServiceClient,
    IBackupProgressService progressService,
    ILogger<Handler> log) : IRequestHandler<Command, Response>
  {
    private const string ContainerName = "backups";
    private const string TableName = "TableBackups";

    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      var backupId = progressService.StartBackup();
      var backupTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
      var partitionKey = $"BackupSet-{backupTimestamp}";

      log.LogInformation("Starting full database export. BackupId: {BackupId}, PartitionKey: {PartitionKey}", 
        backupId, partitionKey);

      // Start background task
      _ = Task.Run(async () => await ExecuteBackupAsync(backupId, partitionKey, cancellationToken), cancellationToken);

      return new Response(backupId, "Backup started successfully");
    }

  private async Task ExecuteBackupAsync(string backupId, string partitionKey, CancellationToken cancellationToken)
  {
    try
    {
      log.LogInformation("=== Starting ExecuteBackupAsync ===");
      log.LogInformation("BackupId: {BackupId}, PartitionKey: {PartitionKey}", backupId, partitionKey);
      
      // Ensure container and table exist
      var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
      log.LogInformation("Got BlobContainerClient for container: {ContainerName}", ContainerName);
      log.LogInformation("BlobServiceClient URI: {Uri}", blobServiceClient.Uri);
      
      try
      {
        log.LogInformation("Attempting to create container if not exists...");
        await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        log.LogInformation("? Container creation check complete");
      }
      catch (Exception ex)
      {
        log.LogError(ex, "? Failed to create/check blob container. Error: {Message}", ex.Message);
        if (ex.InnerException != null)
        {
          log.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
        }
        throw;
      }

      log.LogInformation("TableServiceClient URI: {Uri}", tableServiceClient.Uri);
      var tableClient = tableServiceClient.GetTableClient(TableName);
      log.LogInformation("Got TableClient for table: {TableName}", TableName);
      
      try
      {
        log.LogInformation("Attempting to create table if not exists...");
        await tableClient.CreateIfNotExistsAsync(cancellationToken);
        log.LogInformation("? Table creation check complete");
      }
      catch (Exception ex)
      {
        log.LogError(ex, "? Failed to create/check table. Error: {Message}", ex.Message);
        if (ex.InnerException != null)
        {
          log.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
        }
        throw;
      }

      // Create a new scope for the background task to get a fresh DbContext
      using var scope = serviceScopeFactory.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      // Query database for all table names in the 'budget' schema, excluding migration history
      var tableNames = await db.Database.SqlQueryRaw<string>(
        @"SELECT TABLE_NAME 
          FROM INFORMATION_SCHEMA.TABLES 
          WHERE TABLE_SCHEMA = 'budget' 
          AND TABLE_TYPE = 'BASE TABLE' 
          AND TABLE_NAME != '__EFMigrationsHistory'
          ORDER BY TABLE_NAME"
      ).ToListAsync(cancellationToken);

      log.LogInformation("Found {TableCount} tables to export: {TableNames}", 
        tableNames.Count, string.Join(", ", tableNames));

        int completed = 0;
        int failed = 0;
        var totalTables = tableNames.Count;

        foreach (var tableName in tableNames)
        {
          progressService.UpdateProgress(backupId, totalTables, completed, failed, tableName);
          
          var success = await ExportTableWithRetryAsync(
            db,
            tableName, 
            blobContainerClient, 
            tableClient, 
            partitionKey,
            cancellationToken);

          if (success)
          {
            completed++;
            log.LogInformation("Successfully exported table: {TableName}", tableName);
          }
          else
          {
            failed++;
            log.LogError("Failed to export table after retry: {TableName}", tableName);
            progressService.UpdateProgress(backupId, totalTables, completed, failed, null, 
              $"Failed to export table: {tableName}");
          }
        }

        progressService.CompleteBackup(backupId, totalTables, completed, failed);
        log.LogInformation("Backup completed. BackupId: {BackupId}, Completed: {Completed}, Failed: {Failed}", 
          backupId, completed, failed);
      }
      catch (Exception ex)
      {
        log.LogError(ex, "Fatal error during backup execution. BackupId: {BackupId}", backupId);
        progressService.UpdateProgress(backupId, 0, 0, 0, null, $"Fatal error: {ex.Message}");
      }
    }

    private async Task<bool> ExportTableWithRetryAsync(
      BudgetContext db,
      string tableName,
      BlobContainerClient blobContainerClient,
      TableClient tableClient,
      string partitionKey,
      CancellationToken cancellationToken)
    {
      const int totalAttempts = 2; // Initial attempt + 1 retry
      
      for (int attempt = 1; attempt <= totalAttempts; attempt++)
      {
        try
        {
          log.LogInformation("Exporting table {TableName} (attempt {Attempt}/{TotalAttempts})", 
            tableName, attempt, totalAttempts);

          // Export table data to CSV using raw SQL query
          var csv = await ExportTableDataAsync(db, tableName, cancellationToken);

          // Upload to Blob Storage
          var blobName = $"{partitionKey}/{tableName}.csv";
          var blobClient = blobContainerClient.GetBlobClient(blobName);
          
          using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
          await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);

          // Store metadata in Table Storage
          var sizeInBytes = System.Text.Encoding.UTF8.GetByteCount(csv);
          var entity = new TableEntity(partitionKey, tableName)
          {
            { "BlobName", blobName },
            { "SizeBytes", sizeInBytes },
            { "ExportedAt", DateTime.UtcNow },
            { "Attempt", attempt }
          };

          await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);

          return true; // Success
        }
        catch (Exception ex)
        {
          log.LogWarning(ex, "Attempt {Attempt}/{TotalAttempts} failed for table {TableName}", 
            attempt, totalAttempts, tableName);

          if (attempt == totalAttempts)
          {
            log.LogError("All retry attempts exhausted for table {TableName}", tableName);
            return false; // Failed after all retries
          }

          // Wait before retry
          await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
      }

      return false;
    }

    private async Task<string> ExportTableDataAsync(BudgetContext db, string tableName, CancellationToken cancellationToken)
    {
      // Query all data from the table using raw SQL
      var connectionString = db.Database.GetConnectionString();
      
      using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
      await connection.OpenAsync(cancellationToken);
      
      using var command = connection.CreateCommand();
      command.CommandText = $"SELECT * FROM budget.[{tableName}]";
      
      using var reader = await command.ExecuteReaderAsync(cancellationToken);
      
      // Build CSV from data reader
      var csvBuilder = new System.Text.StringBuilder();
      
      // Create header row from column names
      var columnNames = new List<string>();
      for (int i = 0; i < reader.FieldCount; i++)
      {
        columnNames.Add(reader.GetName(i));
      }
      csvBuilder.AppendLine(string.Join(",", columnNames.Select(EscapeCsvValue)));
      
      // Add data rows
      while (await reader.ReadAsync(cancellationToken))
      {
        var values = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
          var value = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
          values.Add(EscapeCsvValue(value));
        }
        csvBuilder.AppendLine(string.Join(",", values));
      }
      
      return csvBuilder.ToString();
    }

    private static string EscapeCsvValue(string value)
    {
      if (string.IsNullOrEmpty(value))
        return string.Empty;
      
      // Escape quotes and wrap in quotes if value contains comma, quote, or newline
      if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
      {
        return $"\"{value.Replace("\"", "\"\"")}\"";
      }
      
      return value;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/utilities/export-all", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Command());
        return Results.Ok(result);
      }).RequireAuthorization("Admin");
    }
  }
}
