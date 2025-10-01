//using System.ComponentModel.DataAnnotations;

//namespace Budget.DTO;

//public interface IBudgetMaintApiClient
//{
//  Task<IEnumerable<EnvelopeDto>> GetEnvelopesDtoAsync(CancellationToken cancellationToken = default);
//  Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
//  Task<IEnumerable<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);
//  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);
//  Task<EnvelopeDto> AddAsync(EnvelopeDto dto);
//  Task<EnvelopeDto> UpdateAsync(EnvelopeDto dto, CancellationToken cancellationToken = default); // new for editing
//  Task<bool> RemoveEnvelopeAsync(int id, CancellationToken cancellationToken = default);
//}
//public interface IBudgetApiClient
//{
//  Task<IReadOnlyList<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default);
//  Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
//  Task<IReadOnlyList<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);
//  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);
//}

//// Converted from positional records to mutable classes for grid editing compatibility
////public class EnvelopeDto
////{
////  public int Id { get; set; }
////  [Required] public string Name { get; set; } = string.Empty;
////  [Required] public string Description { get; set; } = string.Empty;
////  public decimal Balance { get; set; }
////  public decimal Budget { get; set; }
////  public int CategoryId { get; set; }
////  public int SortOrder { get; set; }
////}

////public class CategoryDto
////{
////  public int Id { get; set; }
////  public string Name { get; set; } = string.Empty;
////  public string Description { get; set; } = string.Empty;
////  public int SortOrder { get; set; }
////}

////public class TransactionDto
////{
////  public int TransactionId { get; set; }
////  public int LineId { get; set; }
////  public string Description { get; set; } = string.Empty;
////  public decimal Amount { get; set; }
////  public DateTime Date { get; set; }
////  public string EnvelopeName { get; set; } = string.Empty;
////}

////public sealed class OneTransactionDetail
////{
////  public int Id { get; set; }
////  public DateTime Date { get; set; }
////  public string Description { get; set; } = string.Empty;
////  public decimal TotalAmount { get; set; }
////  public string EnvelopeName { get; set; } = string.Empty;
////  public string UserInitials { get; set; } = string.Empty;
////  public decimal BalanceAfterTransaction { get; set; }
////  public List<TransactionDto> Details { get; set; } = [];
////}