namespace Budget.Api.Features.Transactions;

public static class GetUnallocated
{
  public sealed record Query : IRequest<IEnumerable<Response>>;
  public sealed record Response(int TransactionId, int LineId, int envelopeId, string envelopeName,string Vendor, string Description, decimal Amount, DateTime Date);

  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {


    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
      var result = await (from td in db.TransactionDetails
          join t in db.Transactions on td.TransactionId equals t.Id
          join e in db.Envelopes on td.EnvelopeId equals e.Id
                          where td.EnvelopeId == -1
          select new Response(t.Id, td.LineId, e.Id, e.Name, t.Vendor,td.Notes, td.Amount, t.Date))
        .ToListAsync(cancellationToken);

      return result;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("transactions/unallocated", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      });
    }
  }
}