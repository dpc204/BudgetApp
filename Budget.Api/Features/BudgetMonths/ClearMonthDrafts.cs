namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Clears draft values for a specific month
/// </summary>
public static class ClearMonthDrafts
{
  public sealed record Command(int AcctPeriod) : IRequest<Response>;
  
  public sealed record Response(bool Success, string Message, int RecordsUpdated);

  /// <summary>
  /// Handles clearing draft values for a specific month
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

      // Clear the draft values
      foreach (var budget in budgetsWithDrafts)
      {
        budget.BudgetDraft = null;
      }

      await db.SaveChangesAsync(cancellationToken);

      return new Response(
        true, 
        $"Cleared draft values for {budgetsWithDrafts.Count} records",
        budgetsWithDrafts.Count);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/budgetmonths/clearmonthdrafts", async (
        [FromServices] ISender sender,
        [FromBody] Command command) =>
      {
        var result = await sender.Send(command);
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}
