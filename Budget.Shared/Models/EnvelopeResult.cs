namespace Budget.Shared.Models;


  public sealed class EnvelopeResult
  {
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int EnvelopeId { get; init; }
    public string EnvelopeName { get; init; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal Budget { get; init; }
  
}
