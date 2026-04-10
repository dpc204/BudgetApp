

namespace Budget.Shared.Models;

public class Cat
{
  public string CategoryId { get; init; } = string.Empty;
  public string CategoryName { get; init; } = string.Empty;
  public int SortOrder { get; set; }
  public CatTypes CatType { get; set; }
}