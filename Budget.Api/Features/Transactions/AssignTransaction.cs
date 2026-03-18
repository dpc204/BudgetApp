using System.Diagnostics;

namespace Budget.Api.Features.Transactions;

public static class AssignTransaction
{
  public sealed record Command(int TransactionId, int LineId, int EnvelopeId, string Vendor, string Description, string Notes, bool HiddenFromAssign = false) : IRequest<bool>;

  public class Handler(BudgetContext db, IMoveEnvelopeBalance moveBalance) : IRequestHandler<Command, bool>
  {
    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
      var transactionDetail = await db.TransactionDetails
        .Include(td => td.Transaction)
        .FirstOrDefaultAsync(td => td.TransactionId == request.TransactionId && td.LineId == request.LineId,
          cancellationToken);

      if(transactionDetail is null)
      {
        return false;
      }

      var unassignedEnvelope = await GetEnvelopeByType.Get(db, EnvelopeTypes.Unassigned, cancellationToken);

      var fromEnvelopeId = transactionDetail.EnvelopeId;

      Debug.WriteLine(
        $"Assigning TransactionId {request.TransactionId} LineId {request.LineId} to EnvelopeId From {transactionDetail.EnvelopeId} to {request.EnvelopeId}");

      // Update TransactionDetail properties
      transactionDetail.EnvelopeId = request.EnvelopeId;
      transactionDetail.Notes = request.Notes;

      // Update Transaction properties (Vendor and Description)
      transactionDetail.Transaction.Vendor = request.Vendor;
      transactionDetail.Transaction.Description = request.Description;

      if(request.EnvelopeId != unassignedEnvelope?.Id)
        transactionDetail.Transaction.TransactionHiddenFromAssign = false;
      else
        transactionDetail.Transaction.TransactionHiddenFromAssign = request.HiddenFromAssign;

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

    if(fromEnvelope == null || toEnvelope == null)
      throw new InvalidOperationException("One or both envelopes do not exist.");

    //if (fromEnvelope.Balance < amountToMove)
    //  throw new InvalidOperationException("Insufficient balance in the source envelope.");

    toEnvelope.Balance += amountToMove;
    fromEnvelope.Balance -= amountToMove;

    await db.SaveChangesAsync();
  }
}