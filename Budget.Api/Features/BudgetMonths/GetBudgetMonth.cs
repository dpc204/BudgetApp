namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Gets budget data for a specific month, ensuring all envelopes are represented
/// </summary>
public static class GetBudgetMonth
{
  public sealed record Query(DateTime Month) : IRequest<IEnumerable<Response>>;
  
  public sealed record Response(
    DateTime BudgetMonthDate,
    int EnvelopeId,
    string EnvelopeName,
    int CategoryId,
    string CategoryName,
    CatTypes CategoryType,
    int SortOrder,
    decimal Budget,
    decimal? BudgetDraft);

  /// <summary>
  /// Handles getting budget data for a month
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {
    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
      // Normalize to first of month
      var firstOfMonth = new DateTime(request.Month.Year, request.Month.Month, 1);

      // Get all envelopes with their categories
      var allEnvelopes = await db.Envelopes
        .AsNoTracking()
        .Include(e => e.Category)
        .OrderBy(e => e.SortOrder)
        .ToListAsync(cancellationToken);

      // Get existing budget data for this month
      var existingBudgets = await db.BudgetMonths
        .AsNoTracking()
        .Where(b => b.BudgetMonthDate == firstOfMonth)
        .ToListAsync(cancellationToken);

      // Build response ensuring all envelopes are included
      var results = new List<Response>();
      
      foreach (var envelope in allEnvelopes)
      {
        var budgetData = existingBudgets.FirstOrDefault(b => b.EnvelopeId == envelope.Id);
        
        results.Add(new Response(
          firstOfMonth,
          envelope.Id,
          envelope.Name,
          envelope.CategoryId,
          envelope.Category.Name,
          envelope.Category.CategoryType,
          envelope.SortOrder,
          budgetData?.Budget ?? 0,
          budgetData?.BudgetDraft
        ));
      }

      return results;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/budgetmonths/{year}/{month}", async (
        [FromServices] ISender sender,
        [FromRoute] int year,
        [FromRoute] int month) =>
      {
        var date = new DateTime(year, month, 1);
        var result = await sender.Send(new Query(date));
        return Results.Ok(result);
      });
    }
  }
}
