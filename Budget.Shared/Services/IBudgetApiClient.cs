using Budget.Shared.Models.Queries;
using static System.Net.WebRequestMethods;

namespace Budget.Shared.Services;

public interface IBudgetApiClient
{
  Task<List<EnvelopeDto>> GetEnvelopesAsync(EnvelopeTypes envelopeType = EnvelopeTypes.All, CancellationToken cancellationToken = default);
  Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<List<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, int startIndex = 0, int pageSize = 0, CancellationToken cancellationToken = default);
  Task<Result<List<EnvelopeTransactionListItem>>> GetFullTransactionsByEnvelopeAsync(int envelopeId, int startIndex = 0, int pageSize = 0, CancellationToken cancellationToken = default);

  Task<Result<List<TransactionDto>>> GetTransactionsUnassignedAsync(CancellationToken cancellationToken = default);
  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);
  Task<UserDetailDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);

  Task<TransactionAddResult> AddTransactionAsync(OneTransactionDetail newTransaction, CancellationToken cancellationToken = default);

   Task<TransactionAddResult> AddMultipleTransactionsAsync(List<OneTransactionDetail> newTransaction, CancellationToken cancellationToken = default);
  Task<List<EnvelopeDto>> UpdateTransactionAsync(OneTransactionDetail transaction, CancellationToken cancellationToken = default);
  Task<List<EnvelopeDto>> VoidTransactionAsync(int transactionId, CancellationToken cancellationToken = default);
  Task<bool> AssignTransactionAsync(int transactionId, int lineId, int envelopeId, string description, CancellationToken cancellationToken = default);
  Task<EnvelopeDto> GetEnvelopeByIdAsync(int envelopeId, CancellationToken cancellationToken = default);
  Task<List<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
 
  // Transaction Import
  Task<int> ImportTransactionsAsync(List<TransactionImportDto> transactions, CancellationToken cancellationToken = default);
  Task<List<TransactionImportDto>> GetTransactionImportsAsync(CancellationToken cancellationToken = default);
  Task<int> ClearTransactionImportsAsync(CancellationToken cancellationToken = default);
  Task<bool> UpdateTransactionImportAsync(int id, bool duplicate, bool keepDuplicate, CancellationToken cancellationToken = default);
  Task<int> UpdateTransactionImportsBatchAsync(List<int> ids, bool duplicate, CancellationToken cancellationToken = default);
  Task<int> LoadTransactionImportsToUnassignedAsync(int accountId, int userId, CancellationToken cancellationToken = default);

  // Maintenance
  Task<string> TriggerAzureSqlBackupAsync(CancellationToken cancellationToken = default);

  // User Options
  Task<UserOptions?> GetUserOptionsAsync(int userId, CancellationToken cancellationToken = default);
  Task<bool> SaveUserOptionsAsync(int userId, UserOptions options, CancellationToken cancellationToken = default);
  Task<AssignQueryResult> GetUnassignedVirtualAsync(AssignQuery query, CancellationToken cancellationToken = default);
}

