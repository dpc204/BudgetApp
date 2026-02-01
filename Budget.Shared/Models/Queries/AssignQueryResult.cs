namespace Budget.Shared.Models.Queries;

public class AssignQueryResult
{
  public List<TransactionDto> Items { get; set; } = new();
  public int TotalCount { get; set; }
}