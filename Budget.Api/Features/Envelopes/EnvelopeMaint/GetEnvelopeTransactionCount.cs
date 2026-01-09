namespace Budget.Api.Features.Envelopes.EnvelopeMaint;

/// <summary>
/// Gets the count of transactions associated with an envelope.
/// Used to determine if an envelope can be safely deleted.
/// </summary>
public static class GetEnvelopeTransactionCount
{
  public sealed record Query(int EnvelopeId) : IRequest<Response>;
  public sealed record Response(int EnvelopeId, int TransactionCount);

  /// <summary>
  /// Handles fetching the transaction count for a specific envelope.
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      var count = await db.TransactionDetails
        .Where(td => td.EnvelopeId == request.EnvelopeId)
        .CountAsync(cancellationToken);

      return new Response(request.EnvelopeId, count);
    }
  }

  /// <summary>
  /// Maps the endpoint routes for getting envelope transaction count.
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/envelopes/maint/{envelopeId}/transaction-count", async (int envelopeId, [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query(envelopeId));
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}
