namespace Budget.Shared.Models;

public class TransactionDto
{
  public int TransactionId { get; set; }
  public int LineId { get; set; }
  public string Description { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public DateTime Date { get; set; }
  public string EnvelopeName { get; set; } = string.Empty;
}