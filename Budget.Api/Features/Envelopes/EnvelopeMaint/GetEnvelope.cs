using Budget.DB;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Envelopes.EnvelopeMaint;

public static class GetAll
{
  public sealed record Query : IRequest<IEnumerable<Response>>;
  public sealed record Response(int Id, string Name, decimal Balance, decimal Budget, int CategoryId, int SortOrder);

  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {
    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)

    {
     var result = await db.Envelopes.AsNoTracking()
       .Select(a=> new Response(a.Id, a.Name, a.Balance, a.Budget, a.CategoryId, a.SortOrder))
         .ToListAsync(cancellationToken);

     // Wrap the single Response in a collection (e.g., array)
     return result;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/envelopes/maint/getall", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      });
    }
  }
}