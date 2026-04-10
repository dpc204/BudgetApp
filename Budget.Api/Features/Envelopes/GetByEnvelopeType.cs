namespace Budget.Api.Features.Envelopes;

public static class GetByEnvelopeType
{
  public sealed record Query(EnvelopeTypes EnvType) : IRequest<EnvelopeDto?>;

  public class Handler(BudgetContext db) : IRequestHandler<Query, EnvelopeDto?>
  {
    public async Task<EnvelopeDto?> Handle(Query request, CancellationToken cancellationToken)
    {
      return await GetEnvelopeByType.Get(db, request.EnvType, cancellationToken);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/envelopes/bytype/{envelopeType}",
        async (EnvelopeTypes envelopeType, [FromServices] ISender sender) =>
        {
          var envelope = await sender.Send(new Query(envelopeType));

          if(envelope == null)
            return Results.NotFound();

          return Results.Ok(envelope);
        }).RequireAuthorization();
    }
  }
}