namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Clears budget values for a specific month
/// </summary>
public static class ClearMonthBudgets
{
  public sealed record Command(int AcctPeriod) : IRequest<Response>;

  public sealed record Response(bool Success, string Message, int RecordsUpdated);

  /// <summary>
  /// Handles clearing budget values for a specific month
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Validate AcctPeriod format
      var year = request.AcctPeriod / 100;
      var month = request.AcctPeriod % 100;

      if(month < 1 || month > 12 || year < 1900)
      {
        return new Response(false, "Invalid accounting period format", 0);
      }

      // Find all budget records for the specified month that are not locked
      var budgetsToUpdate = await db.BudgetMonths
        .Where(b => b.AcctPeriod == request.AcctPeriod && !b.IsBudgetLocked)
        .ToListAsync(cancellationToken);

      // Clear the budget values
      foreach(var budget in budgetsToUpdate)
      {
        budget.Budget = null;
      }

      await db.SaveChangesAsync(cancellationToken);

      return new Response(
        true,
        $"Cleared budget values for {budgetsToUpdate.Count} records",
        budgetsToUpdate.Count);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/budgetmonths/clearmonthbudgets", async (
        [FromServices] ISender sender,
        [FromBody] Command command) =>
      {
        var result = await sender.Send(command);
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}
