namespace Budget.Shared.Services;

/// <summary>
/// API client for utility operations (backup, maintenance)
/// </summary>
public interface IUtilitiesApiClient
{
  // Backup operations
  Task<BackupPlanDto> GetBackupPlanAsync(CancellationToken cancellationToken = default);
  Task<string> TriggerAzureSqlBackupAsync(CancellationToken cancellationToken = default);
  Task<FileDownloadDto> DownloadDatabaseBackupAsync(string fileName, CancellationToken cancellationToken = default);

  // Import/Export operations
  Task<ExportAllResponse> ExportAllTablesAsync(CancellationToken cancellationToken = default);
  Task<BackupStatusDto?> GetBackupStatusAsync(string backupId, CancellationToken cancellationToken = default);
  Task<IEnumerable<BackupSetDto>> GetBackupSetsAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<BackupTableDto>> GetBackupSetDetailsAsync(string partitionKey, CancellationToken cancellationToken = default);
  Task<bool> DeleteBackupSetAsync(string partitionKey, CancellationToken cancellationToken = default);
  Task<FileDownloadDto> DownloadBackupCsvAsync(string blobName, CancellationToken cancellationToken = default);

  // BACPAC history operations
  Task<IEnumerable<BacpacBackupDto>> GetBacpacHistoryAsync(CancellationToken cancellationToken = default);
  Task<string> TriggerBacpacBackupAsync(CancellationToken cancellationToken = default);
  Task<bool> DeleteBacpacBackupAsync(string rowKey, CancellationToken cancellationToken = default);
  Task<FileDownloadDto> DownloadBacpacBackupAsync(string rowKey, CancellationToken cancellationToken = default);
}

