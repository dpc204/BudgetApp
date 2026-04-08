using Budget.Shared.Models.Queries;

namespace Budget.Shared.Services;

/// <summary>
/// API client for transaction-related operations
/// </summary>
public interface ITransactionsApiClient
{
  // Read operations
  Task<List<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, int startIndex = 0, int pageSize = 0, CancellationToken cancellationToken = default);
  Task<Result<List<EnvelopeTransactionListItem>>> GetFullTransactionsByEnvelopeAsync(int envelopeId, int startIndex = 0, int pageSize = 0, CancellationToken cancellationToken = default);
  Task<Result<List<TransactionDto>>> GetTransactionsUnassignedAsync(CancellationToken cancellationToken = default);
  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);
  Task<AssignQueryResult> GetUnassignedVirtualAsync(AssignQuery query, CancellationToken cancellationToken = default);

  // Write operations
  Task<EnvelopeDeltas> TransferEnvelopeFundsAsync(string reason, int fromEnvelopeId, int toEnvelopeId, decimal amount, CancellationToken cancellationToken = default);
  Task<EnvelopeDeltas> AddTransactionAsync(OneTransactionDetail newTransaction, CancellationToken cancellationToken = default);
  Task<EnvelopeDeltas> AddMultipleTransactionsAsync(List<OneTransactionDetail> newTransaction, CancellationToken cancellationToken = default);
  Task<List<EnvelopeUpdate>> UpdateTransactionAsync(OneTransactionDetail transaction, CancellationToken cancellationToken = default);
  Task<List<EnvelopeDto>> VoidTransactionAsync(int transactionId, CancellationToken cancellationToken = default);
  Task<bool> AssignTransactionAsync(int transactionId, int lineId, int envelopeId, string vendor, string description, string notes, bool hiddenFromAssign = false, CancellationToken cancellationToken = default);
  Task<int> ClearHiddenUnassignedAsync(CancellationToken cancellationToken = default);

  // Import/Export operations
  Task<int> ImportTransactionsToStagingAsync(List<TransactionImportDto> transactions, CancellationToken cancellationToken = default);
  Task<List<TransactionImportDto>> GetTransactionImportsAsync(CancellationToken cancellationToken = default);
  Task<int> ClearTransactionImportsAsync(CancellationToken cancellationToken = default);
  Task<bool> UpdateTransactionImportAsync(int id, bool duplicate, bool keepDuplicate, CancellationToken cancellationToken = default);
  Task<int> UpdateTransactionImportsBatchAsync(List<int> ids, bool duplicate, CancellationToken cancellationToken = default);
  Task<int> LoadTransactionImportsToUnassignedAsync(int accountId, int userId, CancellationToken cancellationToken = default);
}
