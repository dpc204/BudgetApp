namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Applies all draft budget values to actual budget values
/// </summary>
public static class ApplyDraftBudgets
{
  public sealed record Command : IRequest<Response>;
  
  public sealed record Response(bool Success, string Message, int RecordsUpdated);

  /// <summary>
  /// Handles applying draft budget values to budget
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    /// <summary>
    /// Applies all non-null draft budget values to their corresponding Budget fields, clears the drafts, and persists the changes.
    /// </summary>
    /// <param name="request">The command request (not used by this handler).</param>
    /// <param name="cancellationToken">Cancellation token used for database operations.</param>
    /// <returns>A <see cref="Response"/> containing a success flag, a message describing how many records were updated, and the count of records updated.</returns>
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Find all budget records with draft values
      var budgetsWithDrafts = await db.BudgetMonths
        .Where(b => b.BudgetDraft != null)
        .ToListAsync(cancellationToken);

      // Apply draft values to budget and clear drafts
      foreach (var budget in budgetsWithDrafts)
      {
        budget.Budget = budget.BudgetDraft;
        budget.BudgetDraft = null;
      }

      await db.SaveChangesAsync(cancellationToken);

      return new Response(
        true,
        $"Applied draft values to {budgetsWithDrafts.Count} budget records",
        budgetsWithDrafts.Count);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/budgetmonths/applydrafts", async (
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Command());
        return Results.Ok(result);
      });
    }
  }
}