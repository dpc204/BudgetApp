using Budget.DB;

namespace Budget.Api.Features.Categories.CategoryMaint;

public static class InsertCategory
{
  public sealed record Command(string Name, string Description, int SortOrder) : IRequest<Response>;
  public sealed record Response(string CategoryId, string Name, string Description, int SortOrder);

  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      var cat = new Category
      {
        CategoryId = Guid.NewGuid().ToString(),
        Name = request.Name,
        Description = request.Description,
        SortOrder = request.SortOrder
      };

      db.Categories.Add(cat);
      await db.SaveChangesAsync(cancellationToken);
      return new Response(cat.CategoryId, cat.Name, cat.Description, cat.SortOrder);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/categories/maint/Insert", async (ISender sender, Command command) =>
      {
        var cat = await sender.Send(command);
        return Results.Created($"categories/maint/{cat.CategoryId}", cat);
      }).RequireAuthorization();
    }
  }
}
