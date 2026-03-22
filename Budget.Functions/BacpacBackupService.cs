using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac;

namespace Budget.Functions;

/// <summary>
/// Service that performs BACPAC backup: exports the database via DacFx,
/// uploads to Azure Blob Storage, records metadata in Azure Table Storage,
/// and deletes backups older than 30 days.
/// </summary>
public class BacpacBackupService(
  BlobServiceClient blobServiceClient,
  TableServiceClient tableServiceClient,
  IConfiguration configuration,
  ILogger<BacpacBackupService> logger)
{
  private const string ContainerName = "bacpac-backups";
  private const string TableName = "BacpacHistory";
  private const int RetentionDays = 30;

  /// <summary>
  /// Runs the full BACPAC backup process:
  /// 1. Exports the database to a .bacpac file using DacFx
  /// 2. Uploads the file to Azure Blob Storage
  /// 3. Records the metadata in Azure Table Storage
  /// 4. Deletes any backups older than 30 days
  /// </summary>
  public async Task RunBackupAsync(CancellationToken cancellationToken = default)
  {
    var connectionString = configuration["SqlConnectionString"]
      ?? throw new InvalidOperationException("SqlConnectionString is not configured.");

    var databaseName = configuration["DatabaseName"];
    if (string.IsNullOrWhiteSpace(databaseName))
    {
      // Derive from connection string
      var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
      databaseName = builder.InitialCatalog;
    }

    if (string.IsNullOrWhiteSpace(databaseName))
      throw new InvalidOperationException("DatabaseName could not be determined from configuration.");

    var timestamp = DateTime.UtcNow;
    var rowKey = timestamp.ToString("yyyyMMdd-HHmmss-fff");
    var blobName = $"{databaseName}-{rowKey}.bacpac";
    var tempPath = Path.Combine(Path.GetTempPath(), blobName);

    logger.LogInformation("Starting BACPAC backup for database {Database}", databaseName);

    try
    {
      // Export using DacFx (note: DacFx does not support cancellation tokens natively;
      // the cancellation token is passed to Task.Run for scheduling purposes only)
      var dac = new DacServices(connectionString);
      logger.LogInformation("Exporting {Database} to temp file {File}", databaseName, tempPath);
      await Task.Run(() => dac.ExportBacpac(tempPath, databaseName), cancellationToken);

      if (!File.Exists(tempPath))
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
      await using (var fileStream = File.OpenRead(tempPath))
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

      logger.LogInformation("BACPAC backup completed successfully. BlobName: {BlobName}", blobName);
    }
    finally
    {
      // Clean up temp file
      try
      {
        if (File.Exists(tempPath)) File.Delete(tempPath);
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Failed to clean up temp file {File}", tempPath);
      }
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
    await foreach (var entity in tableClient.QueryAsync<TableEntity>(
      filter: $"PartitionKey eq '{databaseName}'",
      cancellationToken: cancellationToken))
    {
      var createdAt = entity.GetDateTimeOffset("CreatedAt")?.UtcDateTime ?? DateTime.MinValue;
      if (createdAt < cutoff)
        oldEntities.Add(entity);
    }

    foreach (var entity in oldEntities)
    {
      try
      {
        var blobName = entity.GetString("BlobName") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(blobName))
        {
          var blobClient = containerClient.GetBlobClient(blobName);
          await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
          logger.LogInformation("Deleted old BACPAC blob: {BlobName}", blobName);
        }

        await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: cancellationToken);
        logger.LogInformation("Deleted old BACPAC table entry: {RowKey}", entity.RowKey);
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Failed to delete old BACPAC entry {RowKey}", entity.RowKey);
      }
    }
  }
}
