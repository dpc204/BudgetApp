using Budget.DB;
using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Transactions;

public static class GetByEnvelopeId
{
  public sealed record Query(int EnvelopeId) : IRequest<IEnumerable<Response>>;
  public sealed record Response(int TransactionId, int LineId, string Description, decimal Amount, DateTime Date);

  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {


    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)
    {

      //await db.Transactions
      //  .Where(t => t.EnvelopeId == request.EnvelopeId)
      //  .Select(t => new Response(t.Id, t.Description, t.Amount, t.Date))
      //  .ToListAsync(cancellationToken);

      var result = await (from td in db.TransactionDetails
          join t in db.Transactions on td.TransactionId equals t.Id
          where td.EnvelopeId == request.EnvelopeId
          select new Response(t.Id, td.LineId, t.Description, td.Amount, t.Date))
        .ToListAsync(cancellationToken);

      return result;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/transactions/{envelope}", async ([FromServices] ISender sender, int envelope) =>
      {
        var result = await sender.Send(new Query(envelope));
        return Results.Ok(result);
      });
    }
  }
}