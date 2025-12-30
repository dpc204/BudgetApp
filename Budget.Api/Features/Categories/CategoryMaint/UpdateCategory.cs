namespace Budget.Api.Features.Categories.CategoryMaint;

public static class UpdateCategory
{
  public sealed record Command(string CategoryId, string Name, string Description, int SortOrder) : IRequest<Response?>;
  public sealed record Response(string CategoryId, string Name, string Description, int SortOrder);

  public class Handler(BudgetContext db) : IRequestHandler<Command, Response?>
  {
    public async Task<Response?> Handle(Command request, CancellationToken cancellationToken)
    {
      var entity = await db.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);
      if (entity is null) return null;

      entity.Name = request.Name;
      entity.Description = request.Description;
      entity.SortOrder = request.SortOrder;

      await db.SaveChangesAsync(cancellationToken);

      return new Response(entity.CategoryId, entity.Name, entity.Description, entity.SortOrder);
    }
  }

  public sealed class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/categories/maint/{id}", async (string id, [FromBody] CommandBody body, ISender sender) =>
      {
        if (id != body.CategoryId) return Results.BadRequest("Route id and payload id differ.");
        var result = await sender.Send(new Command(body.CategoryId, body.Name, body.Description, body.SortOrder));
        return result is null ? Results.NotFound() : Results.Ok(result);
      });
    }
  }

  public sealed class CommandBody
  {
    public string CategoryId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
  }
}
