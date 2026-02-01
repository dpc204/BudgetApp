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
  public bool IsVoided { get; set; }
  public string EnvelopeName { get; set; } = string.Empty;
  public int UserId { get; set; }
  public bool WasPotentialDuplicate { get; set; }
}

public class TransactionDetailDto
{
  public int TransactionId { get; set; }
  public int LineId { get; set; }
  public int EnvelopeId { get; set; }
  public string Notes { get; set; } = string.Empty;
  public decimal Amount { get; set; }

}

public class EnvelopeTransactionListItem
{
  public int TransactionId { get; set; }
  public int LineId { get; set; }
  public string Vendor { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public decimal TransAmount { get; set; }
  public DateTime Date { get; set; }
  public bool IsVoided { get; set; }
  public int UserId { get; set; }
  public int EnvelopeId { get; set; }

  public bool WasPotentialDuplicate { get; set; }
  public decimal LineAmount { get; set; }
  public string Notes { get; set; } = string.Empty;
}