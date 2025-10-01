using Budget.DB;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Categories.CategoryMaint;

public static class UpdateCategory
{
  public sealed record Command(int Id, string Name, string Description, int SortOrder) : IRequest<Response?>;
  public sealed record Response(int Id, string Name, string Description, int SortOrder);

  public class Handler(BudgetContext db) : IRequestHandler<Command, Response?>
  {
    public async Task<Response?> Handle(Command request, CancellationToken cancellationToken)
    {
      var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
      if (entity is null) return null;

      entity.Name = request.Name;
      entity.Description = request.Description;
      entity.SortOrder = request.SortOrder;

      await db.SaveChangesAsync(cancellationToken);

      return new Response(entity.Id, entity.Name, entity.Description, entity.SortOrder);
    }
  }

  public sealed class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/categories/maint/{id}", async (int id, [FromBody] CommandBody body, ISender sender) =>
      {
        if (id != body.Id) return Results.BadRequest("Route id and payload id differ.");
        var result = await sender.Send(new Command(body.Id, body.Name, body.Description, body.SortOrder));
        return result is null ? Results.NotFound() : Results.Ok(result);
      });
    }
  }

  public sealed class CommandBody
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
  }
}
