namespace Budget.Shared.Models;


  public sealed class EnvelopeResult
  {
    public string CategoryId { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public int EnvelopeId { get; init; }
    public int EnvelopeSortOrder { get; set; }
    public int CategorySortOrder { get; set; }
    
    public string EnvelopeName { get; init; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal? Budget { get; init; }
    public EnvelopeTypes EnvelopeType { get; set; }
  }
