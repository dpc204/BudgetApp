using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.SqlServer.Dac;

namespace Budget.Api.Features.Utilities.Bacpac;

/// <summary>
/// Performs a BACPAC backup: exports the database via DacFx, uploads to Azure Blob Storage,
/// records metadata in Azure Table Storage, and deletes backups older than 30 days.
/// </summary>
public static class TriggerBacpacBackup
{
  public sealed record Command : IRequest<Response>;

  public sealed record Response(string Message, string BlobName, long SizeBytes);

  /// <summary>
  /// Handles the BACPAC backup operation
  /// </summary>
  public class Handler(
    BudgetContext db,
    BlobServiceClient blobServiceClient,
    TableServiceClient tableServiceClient,
    ILogger<Handler> logger) : IRequestHandler<Command, Response>
  {
    private const string ContainerName = "bacpac-backups";
    private const string TableName = "BacpacHistory";
    private const int RetentionDays = 30;
    private const int TempFileCleanupDelayMinutes = 5;

    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      var conn = db.Database.GetDbConnection();
      var connString = conn.ConnectionString;
      var databaseName = conn.Database;
      var timestamp = DateTime.UtcNow;
      var rowKey = timestamp.ToString("yyyyMMdd-HHmmss-fff");
      var blobName = $"{databaseName}-{rowKey}.bacpac";
      var tempPath = Path.Combine(Path.GetTempPath(), blobName);

      logger.LogInformation("Starting BACPAC backup for database {Database}", databaseName);

      try
      {
        // Export using DacFx (note: DacFx does not support cancellation tokens natively;
        // the cancellation token is passed to Task.Run for scheduling purposes only)
        var dac = new DacServices(connString);
        logger.LogInformation("Exporting {Database} to temp file {File}", databaseName, tempPath);
        await Task.Run(() => dac.ExportBacpac(tempPath, databaseName), cancellationToken);

        if(!File.Exists(tempPath))
        {
          logger.LogError("DacFx export succeeded but file not found: {File}", tempPath);
          throw new InvalidOperationException("Export failed: output file missing.");
        }

        var fileInfo = new FileInfo(tempPath);
        var sizeBytes = fileInfo.Length;
        logger.LogInformation("DacFx export complete. Size: {Size} bytes", sizeBytes);

        // Upload to Azure Blob Storage
        var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);
        await using(var fileStream = File.OpenRead(tempPath))
        {
          await blobClient.UploadAsync(fileStream, overwrite: true, cancellationToken);
        }
        logger.LogInformation("Uploaded BACPAC to blob: {BlobName}", blobName);

        // Record in Azure Table Storage
        var tableClient = tableServiceClient.GetTableClient(TableName);
        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        var entity = new TableEntity(databaseName, rowKey)
        {
          { "DatabaseName", databaseName },
          { "BlobName", blobName },
          { "SizeBytes", sizeBytes },
          { "CreatedAt", timestamp }
        };
        await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
        logger.LogInformation("Recorded BACPAC metadata in table. RowKey: {RowKey}", rowKey);

        // Delete backups older than RetentionDays
        await DeleteOldBackupsAsync(tableClient, containerClient, databaseName, timestamp, cancellationToken);

        return new Response($"BACPAC backup completed successfully.", blobName, sizeBytes);
      }
      finally
      {
        // Clean up temp file after a short delay to allow streaming to complete
        _ = Task.Run(async () =>
        {
          try
          {
            await Task.Delay(TimeSpan.FromMinutes(TempFileCleanupDelayMinutes), CancellationToken.None);
            if(File.Exists(tempPath)) File.Delete(tempPath);
          }
          catch(Exception ex)
          {
            logger.LogWarning(ex, "Failed to clean up temp BACPAC file {TempPath}", tempPath);
          }
        }, CancellationToken.None);
      }
    }

    private async Task DeleteOldBackupsAsync(
      TableClient tableClient,
      BlobContainerClient containerClient,
      string databaseName,
      DateTime now,
      CancellationToken cancellationToken)
    {
      var cutoff = now.AddDays(-RetentionDays);
      logger.LogInformation("Deleting BACPAC backups older than {Cutoff}", cutoff);

      var oldEntities = new List<TableEntity>();
      await foreach(var entity in tableClient.QueryAsync<TableEntity>(
        filter: $"PartitionKey eq '{databaseName}'",
        cancellationToken: cancellationToken))
      {
        var createdAt = entity.GetDateTimeOffset("CreatedAt")?.UtcDateTime ?? DateTime.MinValue;
        if(createdAt < cutoff)
          oldEntities.Add(entity);
      }

      foreach(var entity in oldEntities)
      {
        try
        {
          var blobName = entity.GetString("BlobName") ?? string.Empty;
          if(!string.IsNullOrWhiteSpace(blobName))
          {
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            logger.LogInformation("Deleted old BACPAC blob: {BlobName}", blobName);
          }

          await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: cancellationToken);
          logger.LogInformation("Deleted old BACPAC table entry: {RowKey}", entity.RowKey);
        }
        catch(Exception ex)
        {
          logger.LogWarning(ex, "Failed to delete old BACPAC entry {RowKey}", entity.RowKey);
        }
      }
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/api/maintenance/bacpac/trigger", async (ISender sender) =>
      {
        var result = await sender.Send(new Command());
        return Results.Ok(result);
      })
      .WithName("TriggerBacpacBackup")
      .WithTags("Maintenance")
      .RequireAuthorization("Admin");
    }
  }
}
