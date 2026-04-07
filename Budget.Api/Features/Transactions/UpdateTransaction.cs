using Budget.Shared.Utilities;
using FluentResults;
using NetTopologySuite.GeometriesGraph;

namespace Budget.Api.Features.Transactions;

/// <summary>
/// Updates an existing transaction and its details
/// </summary>
public static class UpdateTransaction
{
  public sealed record Command(OneTransactionDetail Trans) : IRequest<Result<List<EnvelopeUpdate>>>;

  public class Handler(BudgetContext db) : IRequestHandler<Command, Result<List<EnvelopeUpdate>>>
  {
    private readonly List<EnvelopeUpdate> _envelopeUpdates = [];
    public async Task<Result<List<EnvelopeUpdate>>> Handle(Command request, CancellationToken cancellationToken)
    {
      var existingTrans = await db.Transactions
        .Include(t => t.Details)
        .FirstOrDefaultAsync(t => t.Id == request.Trans.Id, cancellationToken);

      if (existingTrans is null)
      {
        return Result.Fail($"Transaction with Id {request.Trans.Id} not found.");
      }

      var origDetails = Clone.ShallowClone(existingTrans.Details);

      // Restore envelope balances from existing details before updating
      await BackoutTransactionFromEnvelopeBalances(existingTrans);

      // Restore account balance
      await BackoutTransactionFromAccountBalances(existingTrans);

      // Remove existing details
      db.TransactionDetails.RemoveRange(existingTrans.Details);
      existingTrans.Details.Clear(); // Clear the collection to prevent duplicate processing

      // Update transaction header
      existingTrans.AccountId = request.Trans.AccountId;
      existingTrans.Date = request.Trans.Date;
      existingTrans.Vendor = request.Trans.Vendor;
      existingTrans.TotalAmount = 0; // Will be recalculated

      // Add new details
      var lineId = 1;
      foreach (var detail in request.Trans.Details)
      {
        var dtl = new TransactionDetail
        {
          TransactionId = existingTrans.Id,
          LineId = lineId++,
          Amount = detail.Amount,
          EnvelopeId = detail.EnvelopeId,
          Notes = detail.Notes
        };
        existingTrans.TotalAmount = detail.Amount;
        existingTrans.Details.Add(dtl);
      }

      SetEnvelopeUpdates(origDetails, existingTrans.Details);

      // Update account with new balance
      await UpdateAccountAsync(existingTrans);

      // Update envelope balances with new details
      var rslt = await UpdateEnvelopeAsync(existingTrans);

      await db.SaveChangesAsync(cancellationToken);
      return Result.Ok(_envelopeUpdates);
    }

    private void SetEnvelopeUpdates(List<TransactionDetail> origDetails, List<TransactionDetail> newDetails)
    {
      // Group by envelope and sum amounts for both original and new details
      var origAmounts = origDetails
        .GroupBy(d => d.EnvelopeId)
        .ToDictionary(g => g.Key, g => g.Sum(d => d.Amount));

      var newAmounts = newDetails
        .GroupBy(d => d.EnvelopeId)
        .ToDictionary(g => g.Key, g => g.Sum(d => d.Amount));

      // Get all unique envelope IDs from both collections
      var allEnvelopeIds = origAmounts.Keys.Union(newAmounts.Keys);

      // Calculate delta for each envelope
      foreach (var envelopeId in allEnvelopeIds)
      {
        var origAmount = origAmounts.GetValueOrDefault(envelopeId, 0m);
        var newAmount = newAmounts.GetValueOrDefault(envelopeId, 0m);
        var delta = newAmount - origAmount;

        _envelopeUpdates.Add(new EnvelopeUpdate(envelopeId, delta));
      }
    }

    Dictionary<int, decimal> _priorBalances = new();
    private async Task BackoutTransactionFromEnvelopeBalances(Transaction trans)
    {
      var grouped = trans.Details.GroupBy(d => d.EnvelopeId);
      foreach (var grp in grouped)
      {
        var env = await db.Envelopes.FindAsync([grp.Key]);
        if (env is null) continue;
        _priorBalances[env.Id] = env.Balance; // Store prior balance for delta update later
        var oldEnvAmount = grp.Sum(d => d.Amount);
        env.Balance = env.Balance - oldEnvAmount; // Add back the amounts
      }
    }

    private async Task BackoutTransactionFromAccountBalances(Transaction trans)
    {
      var acct = await db.BankAccounts.FindAsync([trans.AccountId]);
      if (acct is null) return;
      acct.Balance = acct.Balance - trans.TotalAmount; // Add back the amount
    }

    private async Task<List<EnvelopeDto>> UpdateEnvelopeAsync(Transaction trans)
    {
      var rslt = new List<EnvelopeDto>();

      var grouped = trans.Details.GroupBy(d => d.EnvelopeId);
      foreach (var grp in grouped)
      {
        var env = await db.Envelopes.FindAsync([grp.Key]);
        if (env is null) continue;
        env.LastTransactionDate = DateTime.UtcNow;
        var lastDtl = grp.OrderByDescending(d => d.LineId).First();
        env.LastTransactionDetail = lastDtl;

        var origBalance = _priorBalances.ContainsKey(env.Id) ? _priorBalances[env.Id] : 0m;
        var delta = grp.Sum(d => d.Amount);
        env.Balance += delta;


        rslt.Add(new EnvelopeDto
        {
          Id = env.Id,
          CategoryId = env.CategoryId,
          Name = env.Name,
          Budget = env.Budget,
          Balance = env.Balance,
          Description = env.Description,
          SortOrder = env.SortOrder
        });
      }

      return rslt;
    }

    private async Task UpdateAccountAsync(Transaction trans)
    {
      var acct = await db.BankAccounts.FindAsync([trans.AccountId]);
      if (acct is null) return;
      acct.LastTransactionDate = DateTime.UtcNow;
      acct.LastTransaction = trans;
      acct.Balance -= trans.TotalAmount;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/Transaction/Update", async (ISender sender, Command command) =>
      {
        var result = await sender.Send(command);
        
        return result.IsSuccess
          ? Results.Ok(result.Value)
          : Results.NotFound(new { error = result.Errors });
      }).RequireAuthorization();
    }
  }
}
