using Budget.DB;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Envelopes.EnvelopeMaint;

public static class UpdateEnvelope
{
  public sealed record Command(int Id, string Name, string Description, decimal Balance, decimal Budget, int CategoryId, int SortOrder) : IRequest<Response?>;
  public sealed record Response(int Id, string Name, string Description, decimal Balance, decimal Budget, int CategoryId, int SortOrder);

  public class Handler(BudgetContext db) : IRequestHandler<Command, Response?>
  {
    public async Task<Response?> Handle(Command request, CancellationToken cancellationToken)
    {
      var entity = await db.Envelopes.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);
      if (entity is null) return null;

      entity.Name = request.Name;
      entity.Description = request.Description;
      entity.Balance = request.Balance;
      entity.Budget = request.Budget;
      entity.CategoryId = request.CategoryId;
      entity.SortOrder = request.SortOrder;

      await db.SaveChangesAsync(cancellationToken);

      return new Response(entity.Id, entity.Name, entity.Description, entity.Balance, entity.Budget, entity.CategoryId, entity.SortOrder);
    }
  }

  public sealed class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/envelopes/maint/{id}", async (int id, [FromBody] CommandBody body, ISender sender) =>
      {
        if (id != body.Id) return Results.BadRequest("Route id and payload id differ.");
        var result = await sender.Send(new Command(body.Id, body.Name, body.Description, body.Balance, body.Budget, body.CategoryId, body.SortOrder));
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
    public decimal Budget { get; set; }
    public int CategoryId { get; set; }
    public int SortOrder { get; set; }
  }
}
