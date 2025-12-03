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
    /// <summary>
    /// Updates the draft budget for the specified account period and envelope, creating a new BudgetMonth if none exists.
    /// </summary>
    /// <param name="request">Command carrying the target AcctPeriod, EnvelopeId, and the DraftValue to set.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the asynchronous database operations to complete.</param>
    /// <returns>A Response whose <c>Success</c> is true when the draft was persisted and whose <c>Message</c> describes the outcome.</returns>
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