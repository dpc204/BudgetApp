namespace Budget.Shared.Models;

public class TransactionImportDto
{
  public int Id { get; set; }
  public DateTime Date { get; set; }
  public string Vendor { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public string Notes { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public int EnvelopeId { get; set; }
  public string EnvelopeName { get; set; } = string.Empty;
  public int UserId { get; set; }
  public DateTime ImportedAt { get; set; }
  public bool Duplicate { get; set; } = false;
  public bool KeepDuplicate { get; set; }

  public bool NotDuplicate { get; set; }

  public override bool Equals(object? obj) =>
    obj is TransactionImportDto tran && Id == tran.Id;

  public override int GetHashCode()
  {
    return HashCode.Combine(Id);
  }
}
