namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Updates the draft budget value for a specific envelope in a specific month
/// </summary>
public static class UpdateBudgetDraft
{
  public sealed record Command(int AcctPeriod, int EnvelopeId, decimal? DraftValue) : IRequest<Response>;
  
  public sealed record Response(bool Success, string Message);

  /// <summary>
  /// Handles updating a draft budget value
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
          Budget = 0,
          BudgetDraft = request.DraftValue
        };
        db.BudgetMonths.Add(budgetMonth);
      }
      else
      {
        // Update existing record
        budgetMonth.BudgetDraft = request.DraftValue;
      }

      await db.SaveChangesAsync(cancellationToken);

      return new Response(true, "Draft budget updated successfully");
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/budgetmonths/draft", async (
        [FromServices] ISender sender,
        [FromBody] Command command) =>
      {
        var result = await sender.Send(command);
        return Results.Ok(result);
      });
    }
  }
}
