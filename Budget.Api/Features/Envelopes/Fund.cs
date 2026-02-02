using Budget.Api.Features.Transactions;
using Budget.Shared.Services;
using FluentResults;

namespace Budget.Api.Features.Envelopes;

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

      try
      {
        var incomeEnvelope = await GetEnvelopeByType.Get(db, EnvelopeTypes.Income, cancellationToken);

        // send a response if the income envelope is not found
        if (incomeEnvelope is null)
        {
          return Result.Fail("Income envelope not found. Cannot fund envelopes.");
        }

        // Find all budget records with draft values in current or future months
        envelopesWithFunds = await db.Envelopes
          .Where(b => b.FundAmount != 0)
          .ToListAsync(cancellationToken);

        var fundingAccount =
          (await db.BankAccounts.FirstOrDefaultAsync(a => a.AccountType == AccountTypes.Funding, cancellationToken))
          ?.Id;


        var _newAssignTransactions = new List<OneTransactionDetail>();

        // Move funds from Income to the standard envelopes
        foreach (var toEnvelope in envelopesWithFunds)
        {
          var assignTran = MakeAssignTransaction(toEnvelope, incomeEnvelope, fundingAccount);
          _newAssignTransactions.Add(assignTran);
        }

        // add the new assign transactions using the AddMultipleTransactions handler
        var addMultipleHandler = new AddMultipleTransaction.Handler(db, insertTransactions);
        await addMultipleHandler.Handle(new AddMultipleTransaction.Command(_newAssignTransactions), cancellationToken);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error funding envelopes");
        return Result.Fail(new ExceptionalError(e));
      }

      return Result.Ok(envelopesWithFunds.Count);
    }

    private OneTransactionDetail MakeAssignTransaction(Envelope env, EnvelopeDto? incomeEnvelope, int? fundingAccount)
    {
      ArgumentNullException.ThrowIfNull(incomeEnvelope, nameof(incomeEnvelope));


      ArgumentNullException.ThrowIfNull(fundingAccount, "Funding account not found");

      var rslt = new OneTransactionDetail()
      {
        Date = DateTime.UtcNow,
        TransactionType = TransactionTypes.Funding,
        Description = $"Funding envelope {env.Name}",
        UserId =  userAndOptions.User.Id,
        AccountId = fundingAccount.Value,
        Vendor = "System"
      };


      rslt.Details = new List<TransactionDetailDto>
      {
        new TransactionDetailDto
        {
          EnvelopeId = env.Id,
          Amount = env.FundAmount,
          LineId = 1
        },
        new TransactionDetailDto
        {
          EnvelopeId = incomeEnvelope!.Id,
          Amount = -env.FundAmount,
          LineId = 2
        }
      };

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

        if (result.IsSuccess)
        {
          return Results.Ok(new { fundedCount = result.Value });
        }

        // Extract error messages into a simple string
        var errorMessage = string.Join("; ", result.Errors.Select(e => e.Message));
        return Results.BadRequest(new { error = errorMessage });
      }).RequireAuthorization();
    }
  }
}