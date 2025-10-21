namespace Budget.Shared.Models;

public class TransactionDto
{
  public int TransactionId { get; set; }
  public int LineId { get; set; }
  public string Vendor { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public DateTime Date { get; set; }
  public int EnvelopeId { get; set; }
  public string EnvelopeName { get; set; }
  public int UserId { get; set; }
}