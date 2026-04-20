using Budget.Shared.Services;

namespace Budget.Api.Features.Transactions;


public sealed record Command(string Reason, int FromEnvelopeId, int ToEnvelopeId, decimal Amount) : IRequest<Result<EnvelopeDeltas>>;


public class Handler(BudgetContext db, IUserAndOptions userAndOptions) : IRequestHandler<Command, Result<EnvelopeDeltas>>
{
  //public async Task TransferEnvelopeFundsAsync(string reason, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove)
  //{
  //  await WriteBalanceMovementTransaction(db, reason, fromEnvelopeId, toEnvelopeId, amountToMove);
  //  await db.SaveChangesAsync();
  //}

  public async Task<Result<EnvelopeDeltas>> Handle(Command command, CancellationToken cancellationToken)
  {
    var account = await db.BankAccounts.FirstOrDefaultAsync(a => a.AccountType == AccountTypes.Transfers);
    ArgumentNullException.ThrowIfNull(account, "No transfer account found. Please create an account with the type 'Transfers' to use this feature.");

    var fromEnvName = GetEnvelopeName(command.FromEnvelopeId);
    var toEnvName = GetEnvelopeName(command.ToEnvelopeId);
    var fromDetail = new TransactionDetail() {
      Amount = command.Amount,
      EnvelopeId = command.FromEnvelopeId,
      Notes = $"Transfer to {toEnvName}",
      LineId = 0
    };

    var toDetail = new TransactionDetail() {
      Amount = command.Amount * -1,
      EnvelopeId = command.ToEnvelopeId,
      Notes = $"Transfer from {fromEnvName}",
      LineId = 1
    };

    var moveTransaction = new Transaction {
      Description = command.Reason,
      TotalAmount = command.Amount,
      Vendor = "Transfer",
      AccountId = account.Id,
      UserId = userAndOptions.User.Id,
      Date = DateTime.UtcNow,
      Details = new List<TransactionDetail> { fromDetail, toDetail }
    };
    db.Transactions.Add(moveTransaction);
    await MoveEnvelopeBalance.MoveBalanceDontSave(db, fromDetail.EnvelopeId, toDetail.EnvelopeId, command.Amount);
    await db.SaveChangesAsync();

    var updates = new EnvelopeDeltas()
    {
      new EnvelopeUpdate(command.FromEnvelopeId, command.Amount * -1),
      new EnvelopeUpdate(command.ToEnvelopeId, command.Amount)
    };

    return Result.Ok(updates);

    string GetEnvelopeName(int envId)
    {
      var fromEnv = db.Envelopes.FindAsync(envId).Result;
      var fromEnvName = fromEnv?.Name ?? envId.ToString();
      return fromEnvName;
    }
  }

  /// <summary>
  /// Carter endpoint that exposes the envelope transfer operation over HTTP
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/envelopes/transfer", async (ISender sender, Command command) =>
      {
        var result = await sender.Send(command);

        return result.IsSuccess
          ? Results.Ok(result.Value)
          : Results.NotFound(new { error = result.Errors });
      }).RequireAuthorization();


    }
  }

  /// <summary>
  /// Request body for envelope transfer
  /// </summary>
  public sealed record TransferRequest(string Reason, int FromEnvelopeId, int ToEnvelopeId, decimal Amount);
}