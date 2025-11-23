namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Clears all draft budget values for current and future months
/// </summary>
public static class ClearDraftBudgets
{
  public sealed record Command : IRequest<Response>;
  
  public sealed record Response(bool Success, string Message, int RecordsUpdated);

  /// <summary>
  /// Handles clearing draft budget values
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Get current month (first of month)
      var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

      // Find all budget records with draft values in current or future months
      var budgetsWithDrafts = await db.BudgetMonths
        .Where(b => b.BudgetMonthDate >= currentMonth && b.BudgetDraft != null)
        .ToListAsync(cancellationToken);

      // Clear the draft values
      foreach (var budget in budgetsWithDrafts)
      {
        budget.BudgetDraft = null;
      }

      await db.SaveChangesAsync(cancellationToken);

      return new Response(
        true, 
        $"Cleared draft values for {budgetsWithDrafts.Count} budget records",
        budgetsWithDrafts.Count);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/budgetmonths/cleardrafts", async (
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Command());
        return Results.Ok(result);
      });
    }
  }
}
