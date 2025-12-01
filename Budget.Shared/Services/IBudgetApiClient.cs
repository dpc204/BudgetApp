namespace Budget.Shared.Services;

public interface IBudgetApiClient
{
  Task<List<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default);
  Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<List<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);

  Task<List<TransactionDto>> GetTransactionsUnassignedAsync(CancellationToken cancellationToken = default);
  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);

  Task<List<EnvelopeDto>> AddTransactionAsync(OneTransactionDetail newTransaction, CancellationToken cancellationToken = default);
  Task<List<EnvelopeDto>> UpdateTransactionAsync(OneTransactionDetail transaction, CancellationToken cancellationToken = default);
  Task<bool> AssignTransactionAsync(int transactionId, int lineId, int envelopeId, string description, CancellationToken cancellationToken = default);

  Task<List<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
  Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken cancellationToken = default);

  // Maintenance
  Task<string> TriggerAzureSqlBackupAsync(CancellationToken cancellationToken = default);
}

