using System.ComponentModel.DataAnnotations;
using Budget.Shared.Models;

namespace Budget.Shared.Services;

public interface IBudgetApiClient
{
  Task<List<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default);
  Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<List<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);
  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);

  Task<OneTransactionDetail> AddTransactionAsync(OneTransactionDetail newTransaction, CancellationToken cancellationToken = default);

  Task<List<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
  Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken cancellationToken = default);
}


//public class EnvelopeDto
//{
//  public int Id { get; set; }
//  [Required] public string Name { get; set; } = string.Empty;
//  [Required] public string Description { get; set; } = string.Empty;
//  public decimal Balance { get; set; }
//  public decimal Budget { get; set; }
//  public int CategoryId { get; set; }
//  public int SortOrder { get; set; }
//}

//public class CategoryDto
//{
//  public int Id { get; set; }
//  public string Name { get; set; } = string.Empty;
//  public string Description { get; set; } = string.Empty;
//  public int SortOrder { get; set; }
//}