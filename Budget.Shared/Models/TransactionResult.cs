namespace Budget.Shared.Models;

public sealed record TransactionResult
{
  public DateTime TranDate { get; init; }
  public string Description {  get; init; } = string.Empty;
  public string UserId { get; init; } = string.Empty;

  public int LineId { get; init; }
  public decimal Amount { get; init; }


}
