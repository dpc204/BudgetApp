using Fantum.Mediator;

namespace Budget.Shared.Models.Queries
{
  public class AssignQuery : IRequest<FBResult<AssignQueryResult>>
  {
    public int StartIndex { get; set; }
    public int Count { get; set; }
    public string? Sort { get; set; }
    public bool Descending { get; set; }
    public List<FilterItem>? Filters { get; set; }
    public bool ShowHidden { get; set; } = true;
  }




}