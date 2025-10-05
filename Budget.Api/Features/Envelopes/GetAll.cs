using Budget.DB;
using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Envelopes;


public static class GetAll
{
  public sealed record Query : IRequest<IEnumerable<Response>>;
  public sealed record Response(int Id, string Name, decimal Balance, decimal Budget, int CategoryId, int SortOrder);
 
 public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {


    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken) =>
      await db.Envelopes.AsNoTracking().Join(db.Categories, e => e.CategoryId, c => c.Id, (e, c) => e)
        .Select(e => new Response(e.Id, e.Name, e.Balance, e.Budget, e.CategoryId, e.SortOrder))
        .ToListAsync(cancellationToken);
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/envelopes/getall", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      });
    }
  }
}




