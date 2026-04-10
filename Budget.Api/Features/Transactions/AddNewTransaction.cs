namespace Budget.Api.Features.Transactions;

public static class AddNewTransaction
{
  public sealed record Command(OneTransactionDetail Trans) : IRequest<EnvelopeDeltas>;


  public class Handler(IInsertTransactions inserter) : IRequestHandler<Command, EnvelopeDeltas>
  {
    public async Task<EnvelopeDeltas> Handle(Command request, CancellationToken cancellationToken)
    {
      var trans = await inserter.AddSingleTransaction(request);
      return trans;
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

