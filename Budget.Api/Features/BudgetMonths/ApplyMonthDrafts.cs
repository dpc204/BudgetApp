using Budget.DB;

namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Applies draft values to budget values for a specific month
/// </summary>
public static class ApplyMonthDrafts
{
  public sealed record Command(int AcctPeriod) : IRequest<Response>;
  
  public sealed record Response(bool Success, string Message, int RecordsUpdated);

  /// <summary>
  /// Handles applying draft values to budget for a specific month
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Validate AcctPeriod format
      var year = request.AcctPeriod / 100;
      var month = request.AcctPeriod % 100;

      if (month < 1 || month > 12 || year < 1900)
      {
        return new Response(false, "Invalid accounting period format", 0);
      }

      // Find all budget records with draft values for the specified month
      var budgetsWithDrafts = await db.BudgetMonths
        .Where(b => b.AcctPeriod == request.AcctPeriod && b.BudgetDraft != null)
        .ToListAsync(cancellationToken);

      // Apply draft values to budget and clear drafts (only for unlocked budgets)
      foreach (var budget in budgetsWithDrafts.Where(a => !a.IsBudgetLocked))
      {
        budget.Budget = budget.BudgetDraft;
        budget.BudgetDraft = null;
      }

      await db.SaveChangesAsync(cancellationToken);

      return new Response(
        true,
        $"Applied draft values to {budgetsWithDrafts.Count(a => !a.IsBudgetLocked)} budget records",
        budgetsWithDrafts.Count(a => !a.IsBudgetLocked));
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/budgetmonths/applymonthdrafts", async (
        [FromServices] ISender sender,
        [FromBody] Command command) =>
      {
        var result = await sender.Send(command);
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}
