namespace Budget.Api.Features.Transactions;

public static class AddMultipleTransaction
{
  public sealed record Command(List<OneTransactionDetail> Trans) : IRequest<TransactionAddResult>;


  public class Handler(IInsertTransactions inserter) : IRequestHandler<Command, TransactionAddResult>
  {
    public async Task<TransactionAddResult> Handle(Command request, CancellationToken cancellationToken)
    {
      var trans = await inserter.AddMultipleTransactions(request.Trans);
      return trans;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/Transaction/InsertMulti", async (ISender sender, Command command) =>
      {
        var envelopes = await sender.Send(command);
        return Results.Ok(envelopes);
      }).RequireAuthorization();
    }
  }
}

