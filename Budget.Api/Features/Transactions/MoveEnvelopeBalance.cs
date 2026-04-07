using Budget.Shared.Services;
using Carter;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Transactions;

public interface IMoveEnvelopeBalance
{
  public Task MoveBalance(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove);
}


public class MoveEnvelopeBalance(IUserAndOptions userAndOptions) : IMoveEnvelopeBalance
{
  public async Task MoveBalance(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove)
  {
    await MoveBalanceDontSave(db, fromEnvelopeId, toEnvelopeId, amountToMove);

    await db.SaveChangesAsync();
  }

  private static async Task MoveBalanceDontSave(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove)
  {
    var fromEnvelope = await db.Envelopes.FirstOrDefaultAsync(e => e.Id == fromEnvelopeId);
    var toEnvelope = await db.Envelopes.FirstOrDefaultAsync(e => e.Id == toEnvelopeId);

    if(fromEnvelope == null || toEnvelope == null)
      throw new InvalidOperationException("One or both envelopes do not exist.");

    toEnvelope.Balance += amountToMove;
    fromEnvelope.Balance -= amountToMove;
  }
}