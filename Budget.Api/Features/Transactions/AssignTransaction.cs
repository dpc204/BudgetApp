using System.Diagnostics;

namespace Budget.Api.Features.Transactions;

public static class AssignTransaction
{
  public sealed record Command(int TransactionId, int LineId, int EnvelopeId, string Description) : IRequest<bool>;

  public class Handler(BudgetContext db, IMoveEnvelopeBalance moveBalance) : IRequestHandler<Command, bool>
  {
    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
      var transactionDetail = await db.TransactionDetails
        .FirstOrDefaultAsync(td => td.TransactionId == request.TransactionId && td.LineId == request.LineId,
          cancellationToken);

      if (transactionDetail is null)
      {
        return false;
      }

      var fromEnvelopeId = transactionDetail.EnvelopeId;


      Debug.WriteLine(
        $"Assigning TransactionId {request.TransactionId} LineId {request.LineId} to EnvelopeId From {transactionDetail.EnvelopeId} to {request.EnvelopeId} and Description From {transactionDetail.Notes} to {request.Description}'");
      transactionDetail.EnvelopeId = request.EnvelopeId;
      transactionDetail.Notes = request.Description;
      var toEnvelopeId = transactionDetail.EnvelopeId;
      // Now that the transaction detail is updated, we need to move the balance
      await moveBalance.MoveBalance(db, fromEnvelopeId, toEnvelopeId, transactionDetail.Amount);
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

public interface IMoveEnvelopeBalance
{
  public Task MoveBalance(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove);
}

public class MoveEnvelopeBalance : IMoveEnvelopeBalance
{
  public async Task MoveBalance(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove)
  {
    var fromEnvelope = await db.Envelopes.FirstOrDefaultAsync(e => e.Id == fromEnvelopeId);
    var toEnvelope = await db.Envelopes.FirstOrDefaultAsync(e => e.Id == toEnvelopeId);

    if (fromEnvelope == null || toEnvelope == null)
      throw new InvalidOperationException("One or both envelopes do not exist.");

    if (fromEnvelope.Balance < amountToMove)
      throw new InvalidOperationException("Insufficient balance in the source envelope.");

    toEnvelope.Balance += amountToMove;
    fromEnvelope.Balance -= amountToMove;

    await db.SaveChangesAsync();
  }
}