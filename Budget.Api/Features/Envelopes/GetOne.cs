namespace Budget.Api.Features.Envelopes;

public static class GetOne
{
  public sealed record Query(int EnvelopeId) : IRequest<Response>;

  public sealed record Response(EnvelopeDto? Envelope);

  public class Handler(BudgetContext db) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      var envelope = await db.Envelopes
        .AsNoTracking()
        .Include(env => env.Category)
        .Where(a => a.Id == request.EnvelopeId)
        .Select(env => new EnvelopeDto
        {
          Id = env.Id,
          Name = env.Name,
          FundAmount = env.FundAmount,
          Balance = env.Balance,
          Category = new CategoryDto
          {
            Id = env.Category.Id,
            Name = env.Category.Name
          }
        })
        .FirstOrDefaultAsync(cancellationToken);

      if (envelope == null)
        return new Response(null);

      return new Response(envelope);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/envelopes/{envelopeId}", async (int envelopeId, [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query(envelopeId));
        return Results.Ok(result);
      });
    }
  }
}