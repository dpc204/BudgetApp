namespace Budget.Shared.Models
{
  public class EnvelopeDto
  {
    public int Id { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public CategoryDto Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal? Budget { get; set; }
    public decimal Balance { get; set; }
    public EnvelopeTypes EnvelopeType { get; set; }
    public decimal FundAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
   
  }
}