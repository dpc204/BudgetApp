namespace Budget.DTO;

public interface IBudgetMaintApiClient
{
  Task<IEnumerable<EnvelopeDto>> GetEnvelopesDtoAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);
  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);
  // Changed to return created envelope with server-generated Id
  Task<EnvelopeDto> AddAsync(EnvelopeDto dto);
}
public interface IBudgetApiClient
{
  Task<IReadOnlyList<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);
  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);
}

public sealed record EnvelopeDto(int Id, string Name,string Description, decimal Balance, decimal Budget, int CategoryId, int SortOrder);
public sealed record CategoryDto(int Id, string Name, string Description, int SortOrder);
public sealed record TransactionDto(int TransactionId, int LineId, string Description, decimal Amount, DateTime Date, string EnvelopeName);

public sealed class OneTransactionDetail
{
  public int Id { get; set; }
  public DateTime Date { get; set; }
  public string Description { get; set; } = string.Empty;
  public decimal TotalAmount { get; set; }
  public string EnvelopeName { get; set; }
  public string UserInitials { get; set; } = string.Empty;
  public decimal BalanceAfterTransaction { get; set; }
  public List<TransactionDto> Details { get; set; } = [];
}