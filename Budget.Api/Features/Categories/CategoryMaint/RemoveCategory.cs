using Budget.DB;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Categories.CategoryMaint;

public static class RemoveCategory
{
  public sealed record Command(int Id) : IRequest<bool>;

  public class Handler(BudgetContext db) : IRequestHandler<Command, bool>
  {
    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
      var entity = await db.Categories.FindAsync(new object[] { request.Id }, cancellationToken);
      if (entity is null)
      {
        return false;
      }
      db.Categories.Remove(entity);
      await db.SaveChangesAsync(cancellationToken);
      return true;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapDelete("/categories/maint/{id}", async (ISender sender, int id) =>
      {
        var success = await sender.Send(new Command(id));
        return success ? Results.NoContent() : Results.NotFound($"Category with Id {id} not found");
      });
    }
  }
}
