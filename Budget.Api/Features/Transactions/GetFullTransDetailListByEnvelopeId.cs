namespace Budget.Api.Features.Transactions;

public static class GetFullTransDetailListByEnvelopeId
{
  public sealed record Query(int EnvelopeId, int? StartIndex = null, int? PageSize = null) : IRequest<IEnumerable<Response>>;

  public sealed record Response(FullTransactionDto transDetail);

  public class Handler(BudgetContext db) : IRequestHandler<Query, IEnumerable<Response>>
  {


    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
      //var rslt = db.TransactionDetails.Include(a => a.Transaction)
      //  .AsSplitQuery()
      //  .Where(a => a.EnvelopeId == request.EnvelopeId);


      // I want a linq query that returns a list of transactions with all their details, but only for the transactions that have at least one detail with the specified envelope id. I also want to be able to paginate the results.
      var rslt = db.Transactions
        .Include(a => a.Details)
        .AsSplitQuery()
        .Where(a => a.Details.Any(d => d.EnvelopeId == request.EnvelopeId));




      if(request.StartIndex.HasValue && request.PageSize.HasValue)
          rslt = rslt.Skip(request.StartIndex.Value).Take(request.PageSize.Value);


      // I want to convert the rslt to a list of FullTransactionDto, all the details of the transaction. Do not use a Mapster projection, but instead use a manual mapping to create the FullTransactionDto objects. I can use the Select method to project the Transaction entities to FullTransactionDto objects, and then use the ToListAsync method to execute the query and get the results as a list of FullTransactionDto objects.
      var fullTransDetails = await rslt.Select(a => new FullTransactionDto
      {
        TransactionId = a.Id,
        Date = a.Date,
        //Amount = a.Amount,
        //
        Details = a.Details.Select(d => new TransactionDetailDto
        {
          TransactionId = d.TransactionId,
          EnvelopeId = d.EnvelopeId,
          Amount = d.Amount,
        }).ToList()
      }).ToListAsync(cancellationToken);



      // set the result to be a list of Response objects, where each Response object contains a FullTransactionDto object. I can use the Select method to project the list of FullTransactionDto objects to a list of Response objects, and then return the list of Response objects as the result of the Handle method.
      var response = fullTransDetails.Select(a => new Response(a)).ToList();

      //var temp = rslt.ToListAsync(cancellationToken);










      return response;
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