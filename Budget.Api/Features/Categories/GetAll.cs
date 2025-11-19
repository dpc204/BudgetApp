using Budget.Shared.Enums;

namespace Budget.Api.Features.Categories;

public static class GetByEnvelopeId
{
  public sealed record Query : IRequest<IEnumerable<Response>>;

  public sealed record Response(int Id, string Name,string Description, int SortOrder , CatTypes CatType);

  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {
    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken) =>
      await db.Categories
        .Select(e => new Response(e.Id, e.Name, e.Description, e.SortOrder, e.CategoryType))
        .ToListAsync(cancellationToken);
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/categories/getbyenvelopeid", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      });
    }
  }
}

