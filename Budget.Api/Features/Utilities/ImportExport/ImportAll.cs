using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Budget.Api.Services;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Budget.Api.Features.Utilities.ImportExport;

/// <summary>
/// Imports all database tables from a CSV backup set stored in Azure Blob Storage
/// </summary>
public static class ImportAll
{
  public sealed record Command(string PartitionKey, string TargetDatabase) : IRequest<Response>;

  /// <summary>
  /// Returned immediately — the actual restore runs in the background.
  /// Use the RestoreId to poll /utilities/restore-status/{restoreId}
  /// </summary>
  public sealed record Response(string RestoreId, string Message);

  /// <summary>
  /// Handles full database restore from an Azure Storage backup set
  /// </summary>
  public class Handler(
    BlobServiceClient blobServiceClient,
    TableServiceClient tableServiceClient,
    IConfiguration configuration,
    IRestoreProgressService progressService,
    ILogger<Handler> log) : IRequestHandler<Command, Response>
  {
    private const string ContainerName = "backups";
    private const string TableStorageName = "TableBackups";

    public Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      var restoreId = progressService.StartRestore();
      progressService.AppendLog(restoreId, $"Restore started for backup set: {request.PartitionKey} → target: {request.TargetDatabase}");

      log.LogInformation("Starting ImportAll background task. RestoreId: {RestoreId}, PartitionKey: {PartitionKey}",
        restoreId, request.PartitionKey);

      // Start background task — use CancellationToken.None so it survives after the HTTP response returns
      _ = Task.Run(async () =>
      {
        try
        {
          await ExecuteRestoreAsync(restoreId, request.PartitionKey, request.TargetDatabase, CancellationToken.None);
        }
        catch(Exception ex)
        {
          log.LogError(ex, "Unhandled error in restore background task. RestoreId: {RestoreId}", restoreId);
          progressService.AppendLog(restoreId, $"ERROR: Unhandled error — {ex.Message}");
          progressService.Fail(restoreId, ex.Message);
        }
      });

      return Task.FromResult(new Response(restoreId, "Restore started successfully"));
    }

    private async Task ExecuteRestoreAsync(string restoreId, string partitionKey, string targetDatabase, CancellationToken cancellationToken)
    {
      // Step 1: Resolve target connection string
      var connectionString = GetTargetConnectionString(targetDatabase);
      if(string.IsNullOrEmpty(connectionString))
      {
        progressService.AppendLog(restoreId, $"ERROR: Could not resolve connection string for target database: {targetDatabase}");
        progressService.Fail(restoreId, $"Invalid target database: {targetDatabase}");
        return;
      }

      // Step 2: Read backup metadata from Azure Table Storage
      progressService.AppendLog(restoreId, "Reading backup metadata from Azure Table Storage...");
      var tableEntries = new List<(string TableName, string BlobName)>();

      try
      {
        var tableClient = tableServiceClient.GetTableClient(TableStorageName);
        var filter = $"PartitionKey eq '{partitionKey}'";
        await foreach(var entity in tableClient.QueryAsync<TableEntity>(filter, cancellationToken: cancellationToken))
        {
          var tableName = entity.RowKey;
          var blobName = entity.GetString("BlobName") ?? string.Empty;
          if(!string.IsNullOrEmpty(blobName))
            tableEntries.Add((tableName, blobName));
        }
      }
      catch(Exception ex)
      {
        log.LogError(ex, "Failed to read backup metadata. RestoreId: {RestoreId}", restoreId);
        progressService.AppendLog(restoreId, $"ERROR: Failed to read backup metadata — {ex.Message}");
        progressService.Fail(restoreId, ex.Message);
        return;
      }

      if(tableEntries.Count == 0)
      {
        progressService.AppendLog(restoreId, "ERROR: No tables found in the specified backup set.");
        progressService.Fail(restoreId, "No tables found in backup set");
        return;
      }

      progressService.AppendLog(restoreId, $"Found {tableEntries.Count} tables in backup set.");
      progressService.SetTotal(restoreId, tableEntries.Count);

      // Step 3: Download all CSV data from blob storage
      progressService.AppendLog(restoreId, "Downloading CSV data from Azure Blob Storage...");
      var tableData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

      foreach(var (tableName, blobName) in tableEntries)
      {
        try
        {
          progressService.AppendLog(restoreId, $"Downloading {tableName}.csv...");
          var blobClient = blobContainerClient.GetBlobClient(blobName);
          var downloadResponse = await blobClient.DownloadContentAsync(cancellationToken);
          tableData[tableName] = downloadResponse.Value.Content.ToString();
        }
        catch(Exception ex)
        {
          log.LogError(ex, "Failed to download CSV for table: {TableName}", tableName);
          progressService.AppendLog(restoreId, $"ERROR: Failed to download {tableName}.csv — {ex.Message}");
          progressService.Fail(restoreId, $"Failed to download {tableName}: {ex.Message}");
          return;
        }
      }

      progressService.AppendLog(restoreId, "All CSV files downloaded successfully.");

      // Step 4: Execute the restore in a single SQL transaction
      await RestoreTablesAsync(restoreId, tableData, connectionString, cancellationToken);
    }

    private string GetTargetConnectionString(string targetDatabase)
    {
      var rslt = targetDatabase.Equals("azure", StringComparison.OrdinalIgnoreCase)
        ? configuration["BudgetConnection"] ?? string.Empty
        : configuration["LocalBudgetConnection"] ?? string.Empty;

      if(string.IsNullOrEmpty(rslt)) return string.Empty;

      var builder = new SqlConnectionStringBuilder(rslt) { MultipleActiveResultSets = true };
      return builder.ConnectionString;
    }

    private async Task RestoreTablesAsync(string restoreId, Dictionary<string, string> tableData, string connectionString, CancellationToken cancellationToken)
    {
      using var connection = new SqlConnection(connectionString);

      try
      {
        await connection.OpenAsync(cancellationToken);
        progressService.AppendLog(restoreId, "Connected to target database.");
      }
      catch(Exception ex)
      {
        log.LogError(ex, "Failed to connect to target database. RestoreId: {RestoreId}", restoreId);
        progressService.AppendLog(restoreId, $"ERROR: Failed to connect to target database — {ex.Message}");
        progressService.Fail(restoreId, ex.Message);
        return;
      }

      // Determine which tables actually exist in the target database
      var existingTables = await GetExistingTablesAsync(connection, cancellationToken);

      // Filter tableData to only include tables that exist in the target
      var tablesToRestore = tableData
        .Where(kvp => existingTables.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

      foreach(var tableName in tableData.Keys.Where(t => !existingTables.Contains(t, StringComparer.OrdinalIgnoreCase)))
      {
        progressService.AppendLog(restoreId, $"SKIPPED: Table '{tableName}' does not exist in target database.");
        progressService.IncrementFailed(restoreId);
      }

      if(tablesToRestore.Count == 0)
      {
        progressService.AppendLog(restoreId, "ERROR: No matching tables found in target database. Aborting restore.");
        progressService.Fail(restoreId, "No matching tables found in target database");
        return;
      }

      progressService.SetTotal(restoreId, tablesToRestore.Count);
      progressService.AppendLog(restoreId, $"Restoring {tablesToRestore.Count} tables...");

      using var transaction = connection.BeginTransaction();

      try
      {
        // Disable all FK constraints, logging each table
        progressService.AppendLog(restoreId, "Disabling foreign key constraints...");
        foreach(var tableName in existingTables)
        {
          progressService.AppendLog(restoreId, $"  Disabling constraints on {tableName}...");
          await ExecuteNonQueryAsync(connection, transaction,
            $"ALTER TABLE budget.[{tableName}] NOCHECK CONSTRAINT ALL", cancellationToken);
        }
        progressService.AppendLog(restoreId, "All foreign key constraints disabled.");

        // Delete existing records
        progressService.AppendLog(restoreId, "Deleting existing records...");
        foreach(var tableName in tablesToRestore.Keys)
        {
          progressService.AppendLog(restoreId, $"Deleting records from {tableName}...");
          await ExecuteNonQueryAsync(connection, transaction, $"DELETE FROM budget.[{tableName}]", cancellationToken);
        }

        // Import records from each CSV
        var anyFailed = false;
        foreach(var (tableName, csvContent) in tablesToRestore)
        {
          try
          {
            progressService.AppendLog(restoreId, $"Importing {tableName} table...");
            var rowsImported = await ImportTableFromCsvAsync(connection, transaction, tableName, csvContent, cancellationToken);
            progressService.IncrementCompleted(restoreId);
            progressService.AppendLog(restoreId, $"Imported {rowsImported} record(s) to {tableName} table.");
          }
          catch(Exception ex)
          {
            log.LogError(ex, "Failed to import table: {TableName}", tableName);
            progressService.AppendLog(restoreId, $"ERROR: Failed to import {tableName} — {ex.Message}");
            progressService.IncrementFailed(restoreId);
            anyFailed = true;
          }
        }

        if(anyFailed)
        {
          transaction.Rollback();
          progressService.AppendLog(restoreId, "ERROR: One or more tables failed to import. Transaction rolled back.");
          progressService.Fail(restoreId, "One or more tables failed to import. Transaction rolled back.");
          return;
        }

        // Re-enable FK constraints with validation, logging each table
        progressService.AppendLog(restoreId, "Re-enabling and validating foreign key constraints...");
        foreach(var tableName in existingTables)
        {
          progressService.AppendLog(restoreId, $"  Re-enabling constraints on {tableName}...");
          await ExecuteNonQueryAsync(connection, transaction,
            $"ALTER TABLE budget.[{tableName}] WITH CHECK CHECK CONSTRAINT ALL", cancellationToken);
        }
        progressService.AppendLog(restoreId, "All foreign key constraints re-enabled and validated.");

        transaction.Commit();

        progressService.AppendLog(restoreId, $"Restore completed successfully. {tablesToRestore.Count} table(s) restored.");
        progressService.Complete(restoreId);
        log.LogInformation("ImportAll completed. RestoreId: {RestoreId}, Tables: {Count}", restoreId, tablesToRestore.Count);
      }
      catch(Exception ex)
      {
        log.LogError(ex, "Fatal error during restore. RestoreId: {RestoreId}", restoreId);
        try { transaction.Rollback(); } catch(Exception rbEx) { log.LogError(rbEx, "Error during rollback"); }
        progressService.AppendLog(restoreId, $"FATAL ERROR: {ex.Message}. Transaction rolled back.");
        progressService.Fail(restoreId, ex.Message);
      }
    }

    private static async Task<HashSet<string>> GetExistingTablesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
      var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      using var cmd = connection.CreateCommand();
      cmd.CommandText = @"
        SELECT TABLE_NAME
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'budget'
          AND TABLE_TYPE = 'BASE TABLE'
          AND TABLE_NAME != '__EFMigrationsHistory'";

      using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
      while(await reader.ReadAsync(cancellationToken))
        tables.Add(reader.GetString(0));

      return tables;
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken cancellationToken)
    {
      using var cmd = connection.CreateCommand();
      cmd.Transaction = transaction;
      cmd.CommandText = sql;
      cmd.CommandTimeout = 300;
      await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<int> ImportTableFromCsvAsync(
      SqlConnection connection,
      SqlTransaction transaction,
      string tableName,
      string csvContent,
      CancellationToken cancellationToken)
    {
      if(string.IsNullOrWhiteSpace(csvContent))
      {
        log.LogInformation("No data to import for table: {TableName}", tableName);
        return 0;
      }

      var lines = csvContent.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
      if(lines.Length < 2)
      {
        log.LogInformation("No data rows in CSV for table: {TableName}", tableName);
        return 0;
      }

      var columns = ParseCsvLine(lines[0]);
      if(columns.Count == 0) return 0;

      // Build a DataTable from the CSV data.
      // Note: The ExportAll format does not distinguish between NULL and empty string —
      // both are exported as an empty CSV field.  We treat empty fields as NULL here,
      // which matches the original NULL values.
      var dataTable = new DataTable();
      foreach(var col in columns)
        dataTable.Columns.Add(col, typeof(string));

      for(int rowIdx = 1; rowIdx < lines.Length; rowIdx++)
      {
        var line = lines[rowIdx];
        if(string.IsNullOrWhiteSpace(line)) continue;

        var values = ParseCsvLine(line);
        if(values.Count != columns.Count) continue;

        var row = dataTable.NewRow();
        for(int i = 0; i < columns.Count; i++)
          row[i] = string.IsNullOrEmpty(values[i]) ? (object)DBNull.Value : values[i];

        dataTable.Rows.Add(row);
      }

      if(dataTable.Rows.Count == 0)
        return 0;

      // SqlBulkCopyOptions.KeepIdentity  — preserves identity column values from the CSV
      // SqlBulkCopyOptions.KeepNulls     — inserts NULL rather than applying column defaults
      using var bulkCopy = new SqlBulkCopy(
        connection,
        SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls,
        transaction);

      bulkCopy.DestinationTableName = $"budget.[{tableName}]";
      bulkCopy.BulkCopyTimeout = 300;

      foreach(DataColumn col in dataTable.Columns)
        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);

      await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
      return dataTable.Rows.Count;
    }

    private static List<string> ParseCsvLine(string line)
    {
      var values = new List<string>();
      var current = new System.Text.StringBuilder();
      bool inQuotes = false;

      for(int i = 0; i < line.Length; i++)
      {
        char c = line[i];
        if(inQuotes)
        {
          if(c == '"')
          {
            if(i + 1 < line.Length && line[i + 1] == '"')
            {
              current.Append('"');
              i++; // skip escaped quote
            }
            else
            {
              inQuotes = false;
            }
          }
          else
          {
            current.Append(c);
          }
        }
        else
        {
          if(c == '"')
          {
            inQuotes = true;
          }
          else if(c == ',')
          {
            values.Add(current.ToString());
            current.Clear();
          }
          else
          {
            current.Append(c);
          }
        }
      }

      values.Add(current.ToString());
      return values;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/utilities/import-all", async (
        [FromBody] ImportAllRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new Command(request.PartitionKey, request.TargetDatabase), cancellationToken);
        return Results.Ok(result);
      }).RequireAuthorization("Admin");
    }
  }
}

/// <summary>
/// Request body for importing all tables from a backup set
/// </summary>
public sealed record ImportAllRequest(string PartitionKey, string TargetDatabase);
