namespace Budget.Shared.Models;

public sealed class OneTransactionDetail
{
  public int Id { get; set; }
  public int AccountId { get; set; }
  public DateTime Date { get; set; }
  public PostingStatuses PostingStatus { get; set; }
  public string Vendor { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public decimal TotalAmount { get; set; }
  public int UserId { get; set; }
  public string UserName { get; set; } = string.Empty;
  public bool IsVoided { get; set; }
  public bool WasPotentialDuplicate { get; set; }
  public List<TransactionDetailDto> Details { get; set; } = [];
  public TransactionTypes TransactionType { get; set; }
}