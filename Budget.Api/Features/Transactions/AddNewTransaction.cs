using Budget.Api.Features.Envelopes.EnvelopeMaint;
using Budget.DB;
using Budget.Shared.Models;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Accounts.AccountMaint;

public static class AddNewTransaction
{
  public sealed record Command(OneTransactionDetail Trans) : IRequest;

  public sealed record Response(OneTransactionDetail newTransaction);

  public class Handler(BudgetContext db) : IRequestHandler<Command>
  {
    public async Task Handle(Command request, CancellationToken cancellationToken)
    {
      var trans = CreateTransaction(request);
      db.Transactions.Add(trans);
      
      await UpdateAccountAsync(trans);
      await UpdateEnvelopeAsync(trans).ConfigureAwait(false);

      await db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateEnvelopeAsync(Transaction trans)
    {
      // Group by envelope to ensure only one "last" detail is set per envelope
      var grouped = trans.Details.GroupBy(d => d.EnvelopeId);
      foreach (var grp in grouped)
      {
        var env = await db.Envelopes.FindAsync([grp.Key]);
        if (env is null) continue;
        env.LastTransactionDate = DateTime.UtcNow;
        // pick the highest line id as last within this transaction for the envelope
        var lastDtl = grp.OrderByDescending(d => d.LineId).First();
        env.LastTransactionDetail = lastDtl; // EF will map FK on Envelope
        env.Balance -= grp.Sum(d => d.Amount); // subtract total amount for this envelope
      }
    }

    private async Task UpdateAccountAsync(Transaction trans)
    {
      var acct = await db.BankAccounts.FindAsync([trans.AccountId]);
      if (acct is null) return;
      acct.LastTransactionDate = DateTime.UtcNow;
      acct.LastTransaction = trans; // set navigation, EF will set FK after save
      acct.Balance -= trans.TotalAmount;
    }

    private static Transaction CreateTransaction(Command request)
    {
      var trans = new Transaction()
      {
        AccountId = request.Trans.AccountId,
        Date = request.Trans.Date,
        Vendor = request.Trans.Vendor,
        UserId = request.Trans.UserId,
        UserName = request.Trans.UserName
      };

      var lineId = 1;

      foreach (var detail in request.Trans.Details)
      {
        var dtl = new TransactionDetail()
        {
          LineId = lineId++,
          Amount = detail.Amount,
          EnvelopeId = detail.EnvelopeId,
          Notes = detail.Description
        };

        trans.TotalAmount += detail.Amount;
        trans.Details.Add(dtl);
      }

      return trans;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/Transaction/Insert", async (ISender sender, Command command) =>
      {
        await sender.Send(command);
        return Results.Accepted();
      });
    }
  }
}