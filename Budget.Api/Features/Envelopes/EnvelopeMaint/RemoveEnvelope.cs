using Budget.DB;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Budget.Shared.Models;

namespace Budget.Api.Features.Envelopes.EnvelopeMaint;

public static class RemoveEnvelope
{
  public sealed record Command(int Id) : IRequest<bool>;

  public class Handler(BudgetContext db) : IRequestHandler<Command, bool>
  {
    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
      var entity = await db.Envelopes.FindAsync(new object[] { request.Id }, cancellationToken);
      if (entity is null)
      {
        return false;
      }
      db.Envelopes.Remove(entity);
      await db.SaveChangesAsync(cancellationToken);
      return true;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapDelete("/envelopes/maint/{id}", async (ISender sender, int id) =>
      {
        var success = await sender.Send(new Command(id));
        return success ? Results.NoContent() : Results.NotFound($"Envelope with Id {id} not found");
      });
    }
  }
}