

using Budget.Shared.Enums;

namespace Budget.Shared.Models;

  public class Cat
  {
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;

    public CatTypes CatType { get; set; }
  }