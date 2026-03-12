namespace Budget.Api.Features.Transactions;

public static class GetUnassigned
{
  public sealed record Query : IRequest<Result<IEnumerable<Response>>>;
  public sealed record Response(
    int TransactionId,
    int LineId,
    int EnvelopeId,
    string EnvelopeName,
    string Vendor,
    string Description,
    decimal Amount,
    DateTime Date,
    PostingStatuses PostingStatus);

  public class Handler(BudgetContext db) : IRequestHandler<Query, Result<IEnumerable<Response>>>
  {


    public async Task<Result<IEnumerable<Response>>> Handle(Query request, CancellationToken cancellationToken)
    {
      var unassignedEnvelope = await GetEnvelopeByType.Get(db, EnvelopeTypes.Unassigned, cancellationToken);

      if(unassignedEnvelope is null)
        return Result.FailIf(unassignedEnvelope == null, "System Error: UnassignedEnvelope not defined");

      
      var result = await (from td in db.TransactionDetails
          join t in db.Transactions on td.TransactionId equals t.Id
          join e in db.Envelopes on td.EnvelopeId equals e.Id
                          where td.EnvelopeId == unassignedEnvelope.Id
          select new Response(t.Id, td.LineId, e.Id, e.Name, t.Vendor,td.Notes, td.Amount, t.Date, t.PostingStatus))
        .ToListAsync(cancellationToken);

      return Result.Ok<IEnumerable<Response>>(result);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("transactions/unassigned", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return result.IsSuccess 
          ? Results.Ok(result.Value) 
          : Results.BadRequest(result.Errors);
      }).RequireAuthorization();
    }
  }
}