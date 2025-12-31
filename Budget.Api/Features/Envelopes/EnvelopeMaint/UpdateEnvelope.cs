using Mapster;

namespace Budget.Api.Features.Envelopes.EnvelopeMaint;

public static class UpdateEnvelope
{
  public sealed record Command(EnvelopeDto envelope) : IRequest<Response?>;
  public sealed record Response(EnvelopeDto envelope);

  public class Handler(BudgetContext db) : IRequestHandler<Command, Response?>
  {
    public async Task<Response?> Handle(Command request, CancellationToken cancellationToken)
    {
      var entity = await db.Envelopes.FirstOrDefaultAsync(e => e.Id == request.envelope.Id, cancellationToken);
      if (entity is null) return null;


      entity = request.envelope.Adapt<Envelope>();

      await db.SaveChangesAsync(cancellationToken);

      return new Response(entity.Adapt<EnvelopeDto>());
    }
  }

  public sealed class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/envelopes/maint/{id}", async (int id, [FromBody] EnvelopeDto body, ISender sender) =>
      {
        if (id != body.Id) return Results.BadRequest("Route id and payload id differ.");
        var result = await sender.Send(new Command(body));
        return result is null ? Results.NotFound() : Results.Ok(result);
      });
    }
  }

  public sealed class CommandBody
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal? Budget { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public int SortOrder { get; set; }
  }
}
