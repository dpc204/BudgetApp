namespace Budget.Shared.Services;

public interface IBudgetMaintApiClient
{
  Task<IEnumerable<EnvelopeDto>> GetEnvelopesDtoAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);
  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);
  Task<EnvelopeDto> AddAsync(EnvelopeDto dto);
  Task<EnvelopeDto> UpdateAsync(EnvelopeDto dto, CancellationToken cancellationToken = default); // new for editing
  Task<bool> RemoveEnvelopeAsync(int id, CancellationToken cancellationToken = default);
  Task<EnvelopeImportResult> ImportEnvelopesAsync(string csvContent, CancellationToken cancellationToken = default);
  Task<string> ExportEnvelopesAsync(CancellationToken cancellationToken = default);
  Task<int> GetEnvelopeTransactionCountAsync(int envelopeId, CancellationToken cancellationToken = default);

  // Category maintenance
  Task<CategoryDto> AddCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default);
  Task<CategoryDto> UpdateCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default);
  Task<bool> RemoveCategoryAsync(int id, CancellationToken cancellationToken = default);
  Task<string> ExportCategoriesAsync(CancellationToken cancellationToken = default);

  // Account maintenance
  Task<IEnumerable<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
  Task<BankAccountDto> AddAccountAsync(BankAccountDto dto, CancellationToken cancellationToken = default);
  Task<BankAccountDto> UpdateAccountAsync(BankAccountDto dto, CancellationToken cancellationToken = default);
  Task<bool> RemoveAccountAsync(int id, CancellationToken cancellationToken = default);

  // Backup maintenance
  Task<BackupPlanDto> GetBackupPlanAsync(CancellationToken cancellationToken = default);
}

public sealed record BackupPlanDto(string FileName);
public sealed record EnvelopeImportResult(int ImportedCount, List<string> Errors);