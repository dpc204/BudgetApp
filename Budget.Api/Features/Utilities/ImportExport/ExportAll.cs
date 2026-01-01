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
    BudgetContext db,
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
        // Ensure container and table exist
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var tableClient = tableServiceClient.GetTableClient(TableName);
        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        // Define all tables to export
        var tables = new List<(string Name, Func<Task<string>> ExportFunc)>
        {
          ("Families", () => ExportTableAsync(() => db.Families.IgnoreQueryFilters().ToListAsync(cancellationToken))),
          ("Users", () => ExportTableAsync(() => db.Users.IgnoreQueryFilters().ToListAsync(cancellationToken))),
          ("BankAccounts", () => ExportTableAsync(() => db.BankAccounts.IgnoreQueryFilters().ToListAsync(cancellationToken))),
          ("Categories", () => ExportTableAsync(() => db.Categories.IgnoreQueryFilters().ToListAsync(cancellationToken))),
          ("Envelopes", () => ExportTableAsync(() => db.Envelopes.IgnoreQueryFilters().ToListAsync(cancellationToken))),
          ("Transactions", () => ExportTableAsync(() => db.Transactions.IgnoreQueryFilters().ToListAsync(cancellationToken))),
          ("TransactionDetails", () => ExportTableAsync(() => db.TransactionDetails.ToListAsync(cancellationToken))),
          ("Favorites", () => ExportTableAsync(() => db.Favorites.IgnoreQueryFilters().ToListAsync(cancellationToken))),
          ("BudgetMonths", () => ExportTableAsync(() => db.BudgetMonths.IgnoreQueryFilters().ToListAsync(cancellationToken))),
          ("SavedUserOptions", () => ExportTableAsync(() => db.SavedUserOptions.ToListAsync(cancellationToken)))
        };

        int completed = 0;
        int failed = 0;
        var totalTables = tables.Count;

        foreach (var (tableName, exportFunc) in tables)
        {
          progressService.UpdateProgress(backupId, totalTables, completed, failed, tableName);
          
          var success = await ExportTableWithRetryAsync(
            tableName, 
            exportFunc, 
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
      string tableName,
      Func<Task<string>> exportFunc,
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

          // Export to CSV
          var csv = await exportFunc();

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

    private async Task<string> ExportTableAsync<T>(Func<Task<List<T>>> dataFunc) where T : class
    {
      var data = await dataFunc();
      return CsvExportService.ExportToCsv(data, log: log);
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
      })
      .RequireAuthorization("AdminOnly");
    }
  }
}
