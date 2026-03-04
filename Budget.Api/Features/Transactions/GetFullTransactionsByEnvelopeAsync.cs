namespace Budget.Api.Features.Transactions;

public static class GetFullTransactionsByEnvelopeAsync  
{
  public sealed record Query(int EnvelopeId, int? StartIndex = null, int? PageSize = null)
    : IRequest<IEnumerable<Response>>;

  public sealed record Response(
    int TransactionId,
    string Vendor,
    string Description,
    decimal TransAmount,
    DateTime Date,
    bool IsVoided,
    int UserId,
    bool WasPotentialDuplicate,
    decimal LineAmount,
    string Notes);

  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {
    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
      // I want a linq query that returns a list of transactions with all their details, but only for the transactions that have at least one detail with the specified envelope id. I also want to be able to paginate the results.
      var rslt = db.Transactions
        .Include(a => a.Details)
        .AsSplitQuery()
        .Where(a => a.Details.Any(d => d.EnvelopeId == request.EnvelopeId));


      if (request.StartIndex.HasValue && request.PageSize.HasValue)
        rslt = rslt.Skip(request.StartIndex.Value).Take(request.PageSize.Value);


      var detailsByEnvelope = from r in rslt
        from detail in r.Details
        select new{
          r.Id,
          r.Vendor,
          r.Description,
          r.TotalAmount,
          r.Date,
          r.IsVoided,
          r.UserId,
          r.WasPotentialDuplicate,
          detail.Amount, 
          detail.Notes,
          detail.EnvelopeId};
      
      var resultList = await detailsByEnvelope.Where(a=> a.EnvelopeId == request.EnvelopeId)
        .Select(a => new Response(
          a.Id,
          a.Vendor,
          a.Description,
          a.TotalAmount,
          a.Date,
          a.IsVoided,
          a.UserId,
          a.WasPotentialDuplicate,
          a.Amount,
          a.Notes))
        .ToListAsync(cancellationToken);


      


      // set the result to be a list of Response objects, where each Response object contains a FullTransactionDto object. I can use the Select method to project the list of FullTransactionDto objects to a list of Response objects, and then return the list of Response objects as the result of the Handle method.

      return resultList;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/transactions/getfull/{envelope}", async ([FromServices] ISender sender, int envelope) =>
      {
        var result = await sender.Send(new Query(envelope));
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}