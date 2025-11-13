using System.ComponentModel.DataAnnotations;
using Budget.Shared.Models;

namespace Budget.Shared.Services;

public interface IBudgetApiClient
{
  Task<List<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default);
  Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<List<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);

  Task<List<TransactionDto>> GetTransactionsUnallocatedAsync(CancellationToken cancellationToken = default);
  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);

  Task<List<EnvelopeDto>> AddTransactionAsync(OneTransactionDetail newTransaction, CancellationToken cancellationToken = default);

  Task<List<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
  Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken cancellationToken = default);

  // Maintenance
  Task<string> TriggerAzureSqlBackupAsync(CancellationToken cancellationToken = default);
}

