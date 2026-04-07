using Budget.Shared.Services;

namespace Budget.Api.Features.Transactions;

public interface ITransferEnvelopeFunds
{
  public Task TransferEnvelopeFundsAsync(string reason, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove);
}

public class TransferEnvelopeFunds(BudgetContext db, UserAndOptions userAndOptions) : ITransferEnvelopeFunds
{
  public async Task TransferEnvelopeFundsAsync(string reason, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove)
  {
    await WriteBalanceMovementTransaction(db, reason, fromEnvelopeId, toEnvelopeId, amountToMove);
    await db.SaveChangesAsync();
  }

  private async Task WriteBalanceMovementTransaction(BudgetContext db, string reason, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove)
  {
    var account = await db.BankAccounts.FirstOrDefaultAsync(a => a.AccountType == AccountTypes.Transfers);
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

    var moveTransaction = new Transaction {
      Description = "Transfer: " + reason,
      TotalAmount = amountToMove,
      AccountId = account.Id,
      UserId = userAndOptions.User.Id,
      Date = DateTime.UtcNow,
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
        [FromServices] ITransferEnvelopeFunds service,
        BudgetContext db) =>
      {
        try
        {
          await service.TransferEnvelopeFundsAsync(request.Reason, request.FromEnvelopeId, request.ToEnvelopeId, request.Amount);
          return Results.Ok();
        }
        catch(InvalidOperationException ex)
        {
          return Results.BadRequest(ex.Message);
        }
      }).RequireAuthorization();
    }
  }

  /// <summary>
  /// Request body for envelope transfer
  /// </summary>
  public sealed record TransferRequest(string Reason, int FromEnvelopeId, int ToEnvelopeId, decimal Amount);
}