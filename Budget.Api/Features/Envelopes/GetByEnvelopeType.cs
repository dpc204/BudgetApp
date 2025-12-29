using Budget.Shared.Enums;

namespace Budget.Api.Features.Envelopes;

public static class GetByEnvelopeType
{
  public sealed record Query(EnvelopeTypes EnvType) : IRequest<EnvelopeDto?>;

  public class Handler(BudgetContext db) : IRequestHandler<Query, EnvelopeDto?>
  {
    public async Task<EnvelopeDto?> Handle(Query request, CancellationToken cancellationToken)
    {
      var envelope = await db.Envelopes
        .AsNoTracking()
        .Include(env => env.Category)
        .Where(a => a.EnvelopeType == request.EnvType)
        .Select(env => new EnvelopeDto {
          Id = env.Id,
          Name = env.Name,
          FundAmount = env.FundAmount,
          Balance = env.Balance,
          Category = new CategoryDto {
            Id = env.Category.Id,
            Name = env.Category.Name
          }
        })
        .FirstOrDefaultAsync(cancellationToken);

      return envelope; 
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/envelopes/bytype/{envelopeType}", async (EnvelopeTypes envelopeType, [FromServices] ISender sender) =>
      {
        var envelope = await sender.Send(new Query(envelopeType));
        
        if (envelope == null)
          return Results.NotFound();
          
        return Results.Ok(envelope);
      });
    }
  }
}