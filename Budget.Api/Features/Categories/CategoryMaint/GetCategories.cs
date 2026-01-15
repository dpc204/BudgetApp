namespace Budget.Api.Features.Categories.CategoryMaint;

public static class GetAllCategories
{
  public sealed record Query : IRequest<IEnumerable<Response>>;
  public sealed record Response(string CategoryId, string Name, string Description, int SortOrder);

  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {
    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
      var result = await db.Categories.AsNoTracking()
        .Select(c => new Response(c.CategoryId, c.Name, c.Description, c.SortOrder))
        .ToListAsync(cancellationToken);
      return result;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/categories/maint/getall", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}
