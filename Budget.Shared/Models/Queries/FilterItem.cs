namespace Budget.Shared.Models.Queries;

public class FilterItem
{
  public string? Column { get; set; }
  public string? Operator { get; set; }
  public string? Value { get; set; }
}