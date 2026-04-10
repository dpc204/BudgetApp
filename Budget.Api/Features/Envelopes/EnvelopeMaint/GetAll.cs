namespace Budget.Api.Features.Envelopes.EnvelopeMaint;

public static class GetAll
{
  public sealed record Query : IRequest<IEnumerable<Response>>;
  public sealed record Response(int Id, string Name, string Description, decimal Balance, decimal? Budget, string CategoryId, int SortOrder, EnvelopeTypes EnvelopeType);

  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {
    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)

    {
      var result = await db.Envelopes.Include(e => e.Category) // Eagerly load the Category navigation property
        .AsNoTracking()
        .OrderBy(e => e.Category.SortOrder)
        .ThenBy(e => e.Name)
        .Select(a => new Response(a.Id, a.Name, a.Description, a.Balance, a.Budget, a.CategoryId, a.Category.SortOrder,
          a.EnvelopeType))
        .ToListAsync(cancellationToken);
      return result;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/envelopes/maint/getall", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}