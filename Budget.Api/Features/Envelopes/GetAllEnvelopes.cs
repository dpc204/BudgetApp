namespace Budget.Api.Features.Envelopes;

public static class GetAllEnvelopes
{
  public sealed record Query(EnvelopeTypes EnvelopeType = EnvelopeTypes.All) : IRequest<IEnumerable<Response>>;

  public sealed record Response(
    int Id,
    string Name,
    decimal Balance,
    decimal? Budget,
    string CategoryId,
    int SortOrder,
    EnvelopeTypes EnvelopeType);

  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {
    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
      var query = db.Envelopes.AsNoTracking().Join(db.Categories, e => e.CategoryId, c => c.CategoryId, (e, c) => e);

      if (request.EnvelopeType != EnvelopeTypes.All)
        query = query.Where(e => e.EnvelopeType == request.EnvelopeType);

      query = query.OrderBy(e => e.SortOrder);
      
      var result = query.Select(e => 
        new Response(e.Id, e.Name, e.Balance, e.Budget, e.CategoryId, e.SortOrder, e.EnvelopeType))
        .ToListAsync(cancellationToken);
      return result.Result;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/envelopes/getall/{envelopeType}",
        async (EnvelopeTypes envelopeType, [FromServices] ISender sender) =>
        {
          var result = await sender.Send(new Query(envelopeType));
          return Results.Ok(result);
        }).RequireAuthorization();
    }
  }
}