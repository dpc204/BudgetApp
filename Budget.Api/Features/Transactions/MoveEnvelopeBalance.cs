using Budget.Shared.Services;
using Carter;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Transactions;

public interface IMoveEnvelopeBalance
{
  public Task MoveBalance(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove);
  public Task TransferEnvelopeFundsAsync(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove);
}
public class MoveEnvelopeBalance(IUserAndOptions userAndOptions) : IMoveEnvelopeBalance
{
  public async Task MoveBalance(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove)
  {
    await MoveBalanceDontSave(db, fromEnvelopeId, toEnvelopeId, amountToMove);

    await db.SaveChangesAsync();
  }

  private static async Task MoveBalanceDontSave(BudgetContext db, int fromEnvelopeId, int toEnvelopeId,
    decimal amountToMove)
  {
    var fromEnvelope = await db.Envelopes.FirstOrDefaultAsync(e => e.Id == fromEnvelopeId);
    var toEnvelope = await db.Envelopes.FirstOrDefaultAsync(e => e.Id == toEnvelopeId);

    if(fromEnvelope == null || toEnvelope == null)
      throw new InvalidOperationException("One or both envelopes do not exist.");

    //if (fromEnvelope.Balance < amountToMove)
    //  throw new InvalidOperationException("Insufficient balance in the source envelope.");

    toEnvelope.Balance += amountToMove;
    fromEnvelope.Balance -= amountToMove;
  }

  public async Task TransferEnvelopeFundsAsync(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove)
  {
   await WriteBalanceMovementTransaction(db, fromEnvelopeId, toEnvelopeId, amountToMove);
   await MoveBalanceDontSave(db, fromEnvelopeId, toEnvelopeId, amountToMove);
   await db.SaveChangesAsync();
  }

  private async Task WriteBalanceMovementTransaction(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove)
  {
    var account = await db.BankAccounts.FirstOrDefaultAsync(a=> a.AccountType == AccountTypes.Transfers);
    ArgumentNullException.ThrowIfNull(account, "No transfer account found. Please create an account with the type 'Transfers' to use this feature.");

    var fromDetail = new TransactionDetail() {
      Amount = amountToMove,
      EnvelopeId = fromEnvelopeId,
      Notes = $"Transfer from envelope {fromEnvelopeId}",
      LineId = 0
    };

    var toDetail = new TransactionDetail() {
      Amount = amountToMove * -1,
      EnvelopeId = toEnvelopeId,
      Notes = $"Transfer to envelope {toEnvelopeId}",
      LineId = 1
    };


    var moveTransaction = new Transaction
    {
      TotalAmount = amountToMove,
      AccountId = account.Id,
      UserId = userAndOptions.User.Id,
      Date = DateTime.UtcNow,
      Description = "Transfer between envelopes",
      Details = new List<TransactionDetail> { fromDetail, toDetail }
    };
    db.Transactions.Add(moveTransaction);
  }

  /// <summary>
  /// Carter endpoint that exposes the envelope transfer operation over HTTP
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/envelopes/transfer", async (
        [FromBody] TransferRequest request,
        [FromServices] IMoveEnvelopeBalance service,
        BudgetContext db) =>
      {
        try
        {
          await service.TransferEnvelopeFundsAsync(db, request.FromEnvelopeId, request.ToEnvelopeId, request.Amount);
          return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(ex.Message);
        }
      }).RequireAuthorization();
    }
  }

  /// <summary>
  /// Request body for envelope transfer
  /// </summary>
  public sealed record TransferRequest(int FromEnvelopeId, int ToEnvelopeId, decimal Amount);
}