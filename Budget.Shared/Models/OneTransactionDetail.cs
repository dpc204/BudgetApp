namespace Budget.Shared.Models;

public sealed class OneTransactionDetail
{
  public int Id { get; set; }
  public DateTime Date { get; set; }
  public string Vendor { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public decimal TotalAmount { get; set; }
  public string EnvelopeName { get; set; } = string.Empty;
  public string UserInitials { get; set; } = string.Empty;
  public decimal BalanceAfterTransaction { get; set; }
  public List<TransactionDto> Details { get; set; } = [];
}