using Budget.DB;

namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Checks if there are any draft budget values in the system
/// </summary>
public static class CheckDraftBudgets
{
  public sealed record Query : IRequest<Response>;
  
  public sealed record Response(bool HasDrafts, int DraftCount);

  /// <summary>
  /// Handles checking for draft budget values
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      // Count budget records with draft values
      var draftCount = await db.BudgetMonths
        .Where(b => b.BudgetDraft != null)
        .CountAsync(cancellationToken);

      return new Response(draftCount > 0, draftCount);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/budgetmonths/hasdrafts", async (
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}
