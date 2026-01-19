namespace Budget.Api.Features.Envelopes;


public static class GetAllCategories
{
  public sealed record Query : IRequest<IEnumerable<Response>>;
  public sealed record Response(int Id, string Name, decimal Balance, decimal? Budget, string CategoryId, int SortOrder, EnvelopeTypes envelopeType);
 
 public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {


    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken) =>
      await db.Envelopes.AsNoTracking().Join(db.Categories, e => e.CategoryId, c => c.CategoryId, (e, c) => e)
        .Select(e => new Response(e.Id, e.Name, e.Balance, e.Budget, e.CategoryId, e.SortOrder,e.EnvelopeType))
        .ToListAsync(cancellationToken);
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/envelopes/getall", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}




