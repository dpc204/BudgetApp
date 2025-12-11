namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Updates the lock status for a specific envelope in a specific month
/// </summary>
public static class UpdateBudgetLock
{
  public sealed record Command(int AcctPeriod, int EnvelopeId, bool IsLocked) : IRequest<Response>;
  
  public sealed record Response(bool Success, string Message);

  /// <summary>
  /// Handles updating a budget lock status
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Find or create the budget record
      var budgetMonth = await db.BudgetMonths
        .FirstOrDefaultAsync(
          b => b.AcctPeriod == request.AcctPeriod && b.EnvelopeId == request.EnvelopeId,
          cancellationToken);

      if (budgetMonth == null)
      {
        // Create new budget record
        budgetMonth = new BudgetMonth
        {
          AcctPeriod = request.AcctPeriod,
          EnvelopeId = request.EnvelopeId,
          Budget = null,
          BudgetDraft = null,
          IsBudgetLocked = request.IsLocked
        };
        db.BudgetMonths.Add(budgetMonth);
      }
      else
      {
        // Update existing record
        budgetMonth.IsBudgetLocked = request.IsLocked;
      }

      await db.SaveChangesAsync(cancellationToken);

      return new Response(true, "Budget lock status updated successfully");
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/budgetmonths/lock", async (
        [FromServices] ISender sender,
        [FromBody] Command command) =>
      {
        var result = await sender.Send(command);
        return Results.Ok(result);
      });
    }
  }
}
