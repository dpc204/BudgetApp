namespace Budget.Api.Features.Envelopes;

/// <summary>
/// Updates the fund amount for a specific envelope
/// </summary>
public static class UpdateFundAmount
{
  public sealed record Command(int EnvelopeId, decimal? FundAmount) : IRequest<Response>;
  
  public sealed record Response(bool Success, string Message);

  /// <summary>
  /// Handles updating an envelope's fund amount
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Find the envelope
      var envelope = await db.Envelopes
        .FirstOrDefaultAsync(
          e => e.Id == request.EnvelopeId,
          cancellationToken);

      if (envelope == null)
      {
        return new Response(false, "Envelope not found");
      }

      // Update the fund amount
      envelope.FundAmount = request.FundAmount ?? 0m;

      await db.SaveChangesAsync(cancellationToken);

      return new Response(true, "Fund amount updated successfully");
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/envelopes/fundamount", async (
        [FromServices] ISender sender,
        [FromBody] Command command) =>
      {
        var result = await sender.Send(command);
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}
