namespace Budget.Shared.Models;

public class TransactionImportDto
{
  public int Id { get; set; }
  public DateTime Date { get; set; }
  public string Vendor { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public int EnvelopeId { get; set; }
  public string EnvelopeName { get; set; } = string.Empty;
  public int UserId { get; set; }
  public DateTime ImportedAt { get; set; }
}
