using System.Diagnostics;

namespace Budget.Api.Features.Transactions;

public static class AssignTransaction
{
  public sealed record Command(int TransactionId, int LineId, int EnvelopeId, string Description) : IRequest<bool>;

  public class Handler(BudgetContext db) : IRequestHandler<Command, bool>
  {
    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
      var transactionDetail = await db.TransactionDetails
        .FirstOrDefaultAsync(td => td.TransactionId == request.TransactionId && td.LineId == request.LineId, cancellationToken);

      if (transactionDetail is null)
      {
        return false;
      }
      Debug.WriteLine($"Assigning TransactionId {request.TransactionId} LineId {request.LineId} to EnvelopeId From {transactionDetail.EnvelopeId} to {request.EnvelopeId} and Description From {transactionDetail.Notes} to {request.Description}'");
      transactionDetail.EnvelopeId = request.EnvelopeId;
      transactionDetail.Notes = request.Description;

      await db.SaveChangesAsync(cancellationToken);
      return true;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/transactions/assign", async ([FromServices] ISender sender, Command command) =>
      {
        var result = await sender.Send(command);
        return result ? Results.Ok() : Results.NotFound();
      }).RequireAuthorization();
    }
  }
}
