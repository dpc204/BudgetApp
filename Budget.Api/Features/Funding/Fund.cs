using Budget.Api.Features.Transactions;
using Budget.Shared.Services;

namespace Budget.Api.Features.Funding;

/// <summary>
/// Funds all envelopes based on Envelope.Fund which has previously been set by the user
/// </summary>
public static class Fund
{
  public sealed record Command : IRequest<Result<int>>;

  /// <summary>
  /// Handles funding envelopes based on their FundAmount values
  /// </summary>
  public class Handler(
    BudgetContext db,
    IUserAndOptions userAndOptions,
    ILogger<Handler> logger,
    IInsertTransactions insertTransactions) : IRequestHandler<Command, Result<int>>
  {
    public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
    {
      List<Envelope> envelopesWithFunds = [];
      var result = new Result<int>();
      try
      {
        var incomeEnvelope = await GetEnvelopeByType.Get(db, EnvelopeTypes.Income, cancellationToken);

        if(incomeEnvelope == null)
        {
          result.WithError("No Envelope setup as the Income Envelope");
          logger.LogError("No Envelope setup as the Income Envelope");
        }
        // Find all budget records with draft values in current or future months
        envelopesWithFunds = await db.Envelopes
          .Where(b => b.FundAmount != 0)
          .ToListAsync(cancellationToken);

        if(envelopesWithFunds.Count == 0)
          return Result.Ok().WithSuccess("There were no envelopes with a funding balance");


        var fundingAccount =
          (await db.BankAccounts.FirstOrDefaultAsync(a => a.AccountType == AccountTypes.Funding, cancellationToken))
          ?.Id;

        if(fundingAccount is null)
        {
          result.WithError("FundingAccount was not setup correctly.");
          logger.LogError("FundingAccount was not setup correctly.");
        }

        var _newAssignTransactions = new List<OneTransactionDetail>();

        if(result.IsFailed)
          return result;

        // Move funds from Income to the standard envelopes
        foreach(var toEnvelope in envelopesWithFunds)
        {
          var assignTran = MakeAssignTransaction(toEnvelope, incomeEnvelope, fundingAccount);
          _newAssignTransactions.Add(assignTran);
          toEnvelope.FundAmount = 0;
        }

        // add the new assign transactions using the AddMultipleTransactions handler
        var addMultipleHandler = new AddMultipleTransaction.Handler(insertTransactions);
        var addResult = await addMultipleHandler.Handle(new AddMultipleTransaction.Command(_newAssignTransactions), cancellationToken);

        return result.WithValue(addResult.Count).WithSuccess("Envelopes have been funded");
      }
      catch(Exception e)
      {
        logger.LogError(e, "Error funding envelopes");
        return Result.Fail(new ExceptionalError(e));
      }
    }

    private OneTransactionDetail MakeAssignTransaction(Envelope env, EnvelopeDto? incomeEnvelope, int? fundingAccount)
    {
      ArgumentNullException.ThrowIfNull(incomeEnvelope, nameof(incomeEnvelope));

      if(!fundingAccount.HasValue)
        throw new ArgumentNullException(nameof(fundingAccount));

      var rslt = new OneTransactionDetail() {
        Date = DateTime.UtcNow,
        TransactionType = TransactionTypes.Funding,
        Description = $"Fund: {env.Name}",
        UserId = userAndOptions.User.Id,
        AccountId = fundingAccount.Value,
        Vendor = "Fantum Budget - Fund"
      };


      rslt.Details =
      [
        new()
        {
          EnvelopeId = env.Id,
          Amount = env.FundAmount,
          LineId = 1
        },
        new()
        {
          EnvelopeId = incomeEnvelope!.Id,
          Amount = -env.FundAmount,
          LineId = 2
        }
      ];

      return rslt;
    }
  }


  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/envelopes/fund", async (
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Command());

        if(result.IsSuccess)
        {
          return Results.Ok(FBResult<int>.Success(result.Value));
        }

        // Extract error messages into a simple string
        var errorMessage = string.Join("; ", result.Errors.Select(e => e.Message));
        return Results.BadRequest(FBResult<int>.Failure(errorMessage));
      }).RequireAuthorization();
    }
  }
}