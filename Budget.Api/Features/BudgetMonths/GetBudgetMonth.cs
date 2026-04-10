namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Gets budget data for a specific month, ensuring all envelopes are represented
/// </summary>
public static class GetBudgetMonth
{
  public sealed record Query(int AcctPeriod) : IRequest<IEnumerable<Response>>;

  public sealed record Response(
    int AcctPeriod,
    int EnvelopeId,
    string EnvelopeName,
    string CategoryId,
    string CategoryName,
    CatTypes CategoryType,
    int SortOrder,
    decimal? Budget,
    decimal? BudgetDraft,
    bool IsBudgetLocked,
    decimal FundAmount,
    decimal Balance);

  /// <summary>
  /// Handles getting budget data for a month
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {
    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
      // Get all envelopes with their categories
      var allEnvelopes = await db.Envelopes
        .AsNoTracking()
        .Include(e => e.Category)
        .Where(a => a.EnvelopeType == EnvelopeTypes.Standard || a.EnvelopeType == EnvelopeTypes.Income)
        .OrderBy(e => e.SortOrder)
        .ToListAsync(cancellationToken);

      // Get existing budget data for this month
      var existingBudgets = await db.BudgetMonths
        .AsNoTracking()
        .Where(b => b.AcctPeriod == request.AcctPeriod)
        .ToListAsync(cancellationToken);

      // Build response ensuring all envelopes are included
      var results = new List<Response>();

      foreach(var envelope in allEnvelopes)
      {
        var budgetData = existingBudgets.FirstOrDefault(b => b.EnvelopeId == envelope.Id);

        results.Add(new Response(
          request.AcctPeriod,
          envelope.Id,
          envelope.Name,
          envelope.CategoryId,
          envelope.Category.Name,
          envelope.Category.CategoryType,
          envelope.Category.SortOrder * 1000 + envelope.SortOrder,
          budgetData?.Budget,
          budgetData?.BudgetDraft,
          budgetData?.IsBudgetLocked ?? false,
          envelope.FundAmount,
          envelope.Balance
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
        var acctPeriod = year * 100 + month;
        var result = await sender.Send(new Query(acctPeriod));
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}
