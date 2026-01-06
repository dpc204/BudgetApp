namespace Budget.Shared.Services;

public interface IBudgetMaintApiClient
{
  Task<IEnumerable<EnvelopeDto>> GetEnvelopesDtoAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);
  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);
  Task<EnvelopeDto> AddAsync(EnvelopeDto dto);
  Task<EnvelopeUpdateDto> UpdateAsync(EnvelopeUpdateDto dto, CancellationToken cancellationToken = default); // new for editing
  Task<bool> RemoveEnvelopeAsync(int id, CancellationToken cancellationToken = default);
  Task<ImportResult> ImportEnvelopesAsync(string csvContent, CancellationToken cancellationToken = default);
  Task<string> ExportEnvelopesAsync(CancellationToken cancellationToken = default);
  Task<int> GetEnvelopeTransactionCountAsync(int envelopeId, CancellationToken cancellationToken = default);

  // Category maintenance
  Task<CategoryDto> AddCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default);
  Task<CategoryDto> UpdateCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default);
  Task<bool> RemoveCategoryAsync(string id, CancellationToken cancellationToken = default);
  Task<ImportResult> ImportCategoriesAsync(string csvContent, CancellationToken cancellationToken = default);
  Task<string> ExportCategoriesAsync(CancellationToken cancellationToken = default);

  // Account maintenance
  Task<IEnumerable<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
  Task<BankAccountDto> AddAccountAsync(BankAccountDto dto, CancellationToken cancellationToken = default);
  Task<BankAccountDto> UpdateAccountAsync(BankAccountDto dto, CancellationToken cancellationToken = default);
  Task<bool> RemoveAccountAsync(int id, CancellationToken cancellationToken = default);

  // Backup maintenance
  Task<BackupPlanDto> GetBackupPlanAsync(CancellationToken cancellationToken = default);
  Task<ExportAllResponse> ExportAllTablesAsync(CancellationToken cancellationToken = default);
  Task<BackupStatusDto?> GetBackupStatusAsync(string backupId, CancellationToken cancellationToken = default);
  Task<IEnumerable<BackupSetDto>> GetBackupSetsAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<BackupTableDto>> GetBackupSetDetailsAsync(string partitionKey, CancellationToken cancellationToken = default);
  Task<bool> DeleteBackupSetAsync(string partitionKey, CancellationToken cancellationToken = default);
  Task<FileDownloadDto> DownloadBackupCsvAsync(string blobName, CancellationToken cancellationToken = default);
  Task<FileDownloadDto> DownloadDatabaseBackupAsync(string fileName, CancellationToken cancellationToken = default);
}

public sealed record BackupPlanDto(string FileName);
public sealed record ImportResult(int ImportedCount, List<string> Errors);
public sealed record ExportAllResponse(string BackupId, string Message);
public sealed record BackupStatusDto(
  string BackupId,
  DateTime StartTime,
  DateTime? EndTime,
  int TotalTables,
  int CompletedTables,
  int FailedTables,
  string? CurrentTable,
  string? ErrorMessage,
  bool IsComplete);

public sealed record FileDownloadDto(byte[] Content, string FileName, string ContentType);
