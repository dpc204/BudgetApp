namespace Budget.Api.Features.Transactions;

public static class AddNewTransaction
{
  public sealed record Command(OneTransactionDetail Trans) : IRequest<List<EnvelopeUpdate>>;


  public class Handler( IInsertTransactions inserter) : IRequestHandler<Command, List<EnvelopeUpdate>>
  {
    public async Task<List<EnvelopeUpdate>> Handle(Command request, CancellationToken cancellationToken)
    {
      var trans = await inserter.AddSingleTransaction(request);
      var rslt = trans.EnvelopeUpdates;
      return rslt;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/Transaction/Insert", async (ISender sender, Command command) =>
      {
        var envelopes = await sender.Send(command);
        return Results.Ok(envelopes);
      }).RequireAuthorization();
    }
  }
}

