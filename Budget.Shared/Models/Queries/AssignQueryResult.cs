namespace Budget.Shared.Models.Queries;

public class AssignQueryResult
{
  public List<TransactionDto> Items { get; set; } = [];
  public int TotalCount { get; set; }
}