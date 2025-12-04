using Budget.Api.Features.Envelopes.EnvelopeMaint;

namespace Budget.Api.Features.Transactions;

/// <summary>
/// Voids an existing transaction, reversing its effects on account and envelope balances
/// </summary>
public static class VoidTransaction
{
  public sealed record Command(int TransactionId) : IRequest<List<EnvelopeDto>>;

  /// <summary>
  /// Handles voiding a transaction by setting IsVoided flag and reversing balance changes
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, List<EnvelopeDto>>
  {
    public async Task<List<EnvelopeDto>> Handle(Command request, CancellationToken cancellationToken)
    {
      var existingTrans = await db.Transactions
        .Include(t => t.Details)
        .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

      if (existingTrans is null)
      {
        throw new InvalidOperationException($"Transaction with Id {request.TransactionId} not found.");
      }

      if (existingTrans.IsVoided)
      {
        throw new InvalidOperationException($"Transaction {request.TransactionId} is already voided.");
      }

      // Set void flag
      existingTrans.IsVoided = true;

      // Deduct the full amount from the account balance (reverses the original deduction)
      await ReverseAccountBalanceAsync(existingTrans);

      // Deduct amounts from envelope balances (reverses the original deduction)
      var rslt = await ReverseEnvelopeBalancesAsync(existingTrans);

      await db.SaveChangesAsync(cancellationToken);
      return rslt;
    }

    private async Task ReverseAccountBalanceAsync(Transaction trans)
    {
      var acct = await db.BankAccounts.FindAsync([trans.AccountId]);
      if (acct is null) return;
      
      // Deduct the amount (reverses the original deduction which subtracted from balance)
      acct.Balance -= trans.TotalAmount;
    }

    private async Task<List<EnvelopeDto>> ReverseEnvelopeBalancesAsync(Transaction trans)
    {
      var rslt = new List<EnvelopeDto>();

      var grouped = trans.Details.GroupBy(d => d.EnvelopeId);
      foreach (var grp in grouped)
      {
        var env = await db.Envelopes.FindAsync([grp.Key]);
        if (env is null) continue;

        // Deduct the sum of amounts (reverses the original deduction which subtracted from balance)
        env.Balance -= grp.Sum(d => d.Amount);

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
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/Transaction/Void", async (ISender sender, Command command) =>
      {
        var envelopes = await sender.Send(command);
        return Results.Ok(envelopes);
      });
    }
  }
}
