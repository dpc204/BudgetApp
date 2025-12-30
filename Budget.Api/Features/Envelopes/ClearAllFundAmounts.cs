namespace Budget.Api.Features.Envelopes;

/// <summary>
/// Clears all fund amounts across all envelopes
/// </summary>
public static class ClearAllFundAmounts
{
  public sealed record Command : IRequest<Response>;
  
  public sealed record Response(bool Success, string Message, int RecordsUpdated);

  /// <summary>
  /// Handles clearing all envelope fund amounts
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Get all envelopes with non-zero fund amounts
      var envelopes = await db.Envelopes
        .Where(e => e.FundAmount != 0)
        .ToListAsync(cancellationToken);

      // Clear all fund amounts
      foreach (var envelope in envelopes)
      {
        envelope.FundAmount = 0m;
      }

      await db.SaveChangesAsync(cancellationToken);

      return new Response(true, $"Cleared {envelopes.Count} fund amounts successfully", envelopes.Count);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/envelopes/clearallfundamounts", async (
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Command());
        return Results.Ok(result);
      });
    }
  }
}
