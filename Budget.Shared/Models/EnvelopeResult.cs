namespace Budget.Shared.Models;


  public sealed record EnvelopeResult
  {
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int EnvelopeId { get; init; }
    public string EnvelopeName { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public decimal Budget { get; init; }
  
}
