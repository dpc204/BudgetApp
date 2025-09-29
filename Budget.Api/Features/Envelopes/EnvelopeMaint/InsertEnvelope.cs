using Budget.DB;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
  using Budget.Shared.Models;

namespace Budget.Api.Features.Envelopes.EnvelopeMaint;



public static class InsertEnvelope
{
  public sealed record Command(string Name, string Description, decimal balance, decimal Budget, int CategoryId, int SortOrder) : IRequest<Response>;
  public sealed record Response(int Id, string Name, string Description, decimal Balance, decimal Budget, int CategoryId, int SortOrder);

  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)

    {
      var env = new Envelope
      {
        Name = request.Name,
        Description = request.Description,
        Balance = request.balance,
        Budget = request.Budget,
        CategoryId = request.CategoryId,
        SortOrder = request.SortOrder
      };




      db.Envelopes.Add(env);
     await db.SaveChangesAsync(cancellationToken);
     return new Response(env.Id, env.Name, env.Description, env.Balance, env.Budget, env.CategoryId, env.SortOrder);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/envelopes/maint/Insert", async (ISender sender, Command command) =>
      {
        var env = await sender.Send(command);
        return Results.Created($"envelopes/maint/{env.Id}", env);
      });
    }
  }
}