using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Budget.Api.Features.Utilities.ImportExport;

/// <summary>
/// Imports all database tables from a CSV backup set stored in Azure Blob Storage
/// </summary>
public static class ImportAll
{
  public sealed record Command(string PartitionKey, string TargetDatabase) : IRequest<Response>;

  public sealed record Response(bool Success, string Message, int TablesRestored, int TablesFailed, List<string> Errors);

  /// <summary>
  /// Handles full database restore from an Azure Storage backup set
  /// </summary>
  public class Handler(
    BlobServiceClient blobServiceClient,
    TableServiceClient tableServiceClient,
    IConfiguration configuration,
    ILogger<Handler> log) : IRequestHandler<Command, Response>
  {
    private const string ContainerName = "backups";
    private const string TableStorageName = "TableBackups";

    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      log.LogInformation("Starting ImportAll for PartitionKey: {PartitionKey}, TargetDatabase: {TargetDatabase}",
        request.PartitionKey, request.TargetDatabase);

      // Get all table backup entries for this partition key from Azure Table Storage
      var tableClient = tableServiceClient.GetTableClient(TableStorageName);
      var tableEntries = new List<(string TableName, string BlobName)>();

      try
      {
        var filter = $"PartitionKey eq '{request.PartitionKey}'";
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
        log.LogError(ex, "Failed to retrieve backup set entries from Azure Table Storage");
        return new Response(false, $"Failed to retrieve backup set: {ex.Message}", 0, 0, [$"Failed to retrieve backup set: {ex.Message}"]);
      }

      if(tableEntries.Count == 0)
        return new Response(false, "No tables found in the specified backup set", 0, 0, ["No tables found in backup set"]);

      log.LogInformation("Found {Count} tables to restore", tableEntries.Count);

      // Download all CSV data from blob storage
      var tableData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

      foreach(var (tableName, blobName) in tableEntries)
      {
        try
        {
          var blobClient = blobContainerClient.GetBlobClient(blobName);
          var downloadResponse = await blobClient.DownloadContentAsync(cancellationToken);
          tableData[tableName] = downloadResponse.Value.Content.ToString();
          log.LogInformation("Downloaded CSV for table: {TableName}", tableName);
        }
        catch(Exception ex)
        {
          log.LogError(ex, "Failed to download CSV for table: {TableName}", tableName);
          return new Response(false, $"Failed to download backup for table {tableName}: {ex.Message}", 0, tableEntries.Count, [$"Failed to download {tableName}: {ex.Message}"]);
        }
      }

      // Resolve the target connection string
      var connectionString = GetTargetConnectionString(request.TargetDatabase);
      if(string.IsNullOrEmpty(connectionString))
        return new Response(false, $"Could not determine connection string for target database: {request.TargetDatabase}", 0, 0, [$"Invalid target database: {request.TargetDatabase}"]);

      return await RestoreTablesAsync(tableData, connectionString, cancellationToken);
    }

    private string GetTargetConnectionString(string targetDatabase)
    {
      var rslt = String.Empty;
      if(targetDatabase.Equals("azure", StringComparison.OrdinalIgnoreCase))
        rslt =configuration["BudgetConnection"] ?? string.Empty;
      else
      rslt = configuration["LocalBudgetConnection"] ?? string.Empty;

      var builder = new SqlConnectionStringBuilder(rslt) {
        MultipleActiveResultSets = true
      };
      return builder.ConnectionString;

    }

    private async Task<Response> RestoreTablesAsync(Dictionary<string, string> tableData, string connectionString, CancellationToken cancellationToken)
    {
      var errors = new List<string>();
      int tablesRestored = 0;
      int tablesFailed = 0;

      using var connection = new SqlConnection(connectionString);
      await connection.OpenAsync(cancellationToken);

      using var transaction = connection.BeginTransaction();

      try
      {
        // Step 1: Disable all FK constraints on budget schema tables
        log.LogInformation("Disabling foreign key constraints");
        await ExecuteNonQueryAsync(connection, transaction, @"
          DECLARE @sql NVARCHAR(MAX) = N'';
          SELECT @sql += 'ALTER TABLE budget.[' + t.TABLE_NAME + '] NOCHECK CONSTRAINT ALL;' + CHAR(13)
          FROM INFORMATION_SCHEMA.TABLES t
          WHERE t.TABLE_SCHEMA = 'budget'
            AND t.TABLE_TYPE = 'BASE TABLE'
            AND t.TABLE_NAME != '__EFMigrationsHistory';
          EXEC sp_executesql @sql;", cancellationToken);

        // Step 2: Delete all records from each table
        log.LogInformation("Deleting existing records from {Count} tables", tableData.Count);
        foreach(var tableName in tableData.Keys)
        {
          await ExecuteNonQueryAsync(connection, transaction, $"DELETE FROM budget.[{tableName}]", cancellationToken);
          log.LogInformation("Deleted records from: {TableName}", tableName);
        }

        // Step 3: Import records from CSV into each table
        foreach(var (tableName, csvContent) in tableData)
        {
          try
          {
            var rowsImported = await ImportTableFromCsvAsync(connection, transaction, tableName, csvContent, cancellationToken);
            tablesRestored++;
            log.LogInformation("Imported {RowCount} rows into table: {TableName}", rowsImported, tableName);
          }
          catch(Exception ex)
          {
            log.LogError(ex, "Failed to import table: {TableName}", tableName);
            errors.Add($"Failed to import {tableName}: {ex.Message}");
            tablesFailed++;
          }
        }

        if(tablesFailed > 0)
        {
          transaction.Rollback();
          return new Response(false, $"Restore failed: {tablesFailed} table(s) had errors. Transaction rolled back.", tablesRestored, tablesFailed, errors);
        }

        // Step 4: Re-enable all FK constraints with validation
        log.LogInformation("Re-enabling foreign key constraints");
        await ExecuteNonQueryAsync(connection, transaction, @"
          DECLARE @sql NVARCHAR(MAX) = N'';
          SELECT @sql += 'ALTER TABLE budget.[' + t.TABLE_NAME + '] WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13)
          FROM INFORMATION_SCHEMA.TABLES t
          WHERE t.TABLE_SCHEMA = 'budget'
            AND t.TABLE_TYPE = 'BASE TABLE'
            AND t.TABLE_NAME != '__EFMigrationsHistory';
          EXEC sp_executesql @sql;", cancellationToken);

        transaction.Commit();
        log.LogInformation("ImportAll completed successfully. Tables restored: {Count}", tablesRestored);
        return new Response(true, $"Successfully restored {tablesRestored} tables", tablesRestored, 0, []);
      }
      catch(Exception ex)
      {
        log.LogError(ex, "Fatal error during restore, rolling back transaction");
        try { transaction.Rollback(); } catch(Exception rbEx) { log.LogError(rbEx, "Error during rollback"); }
        return new Response(false, $"Restore failed: {ex.Message}", tablesRestored, tablesFailed + 1, [.. errors, $"Fatal error: {ex.Message}"]);
      }
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
      // which matches the original NULL values.  Any columns that legitimately stored
      // empty strings will be restored as NULL; this is an inherent limitation of the
      // current CSV backup format.
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
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
      }).RequireAuthorization("Admin");
    }
  }
}

/// <summary>
/// Request body for importing all tables from a backup set
/// </summary>
public sealed record ImportAllRequest(string PartitionKey, string TargetDatabase);
