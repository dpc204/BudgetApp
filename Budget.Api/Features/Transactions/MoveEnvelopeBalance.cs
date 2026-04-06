namespace Budget.Api.Features.Transactions;

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