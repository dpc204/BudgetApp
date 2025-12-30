
namespace Budget.Shared.Models
{
  public class CategoryDto
  {
    public string CategoryId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<EnvelopeDto> Envelopes { get; set; } = [];
    public CatTypes CatType { get; set; }

  
  }
}