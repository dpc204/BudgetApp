namespace Budget.Shared.Models
{
  public record EnvelopeIdName(
    int EnvelopeId,
    string CategoryName,
    string EnvelopeName,
    int CategorySortOrder,
    int EnvelopeSortOrder);
}