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
      var trans = InsertTransaction(request);
      await UpdateAccountAsync(trans);

      await UpdateEnvelopeAsync(trans).ConfigureAwait(false);
      db.Transactions.Add(trans);
      await db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateEnvelopeAsync(Transaction trans)
    {
      foreach (var dtl in trans.Details)
      {
        var find = db.Envelopes.FindAsync([dtl.EnvelopeId]);
        var env = await find;

        env?.Balance -= trans.TotalAmount;
      }
    }

    private async Task UpdateAccountAsync(Transaction trans)
    {
      var find = db.BankAccounts.FindAsync([trans.AccountId]);
      var acct = await find;

      acct?.Balance -= trans.TotalAmount;
    }

    private static Transaction InsertTransaction(Command request)
    {
      var trans = new Transaction()
      {
        AccountId = request.Trans.AccountId,
        Date = request.Trans.Date,
        Vendor = request.Trans.Vendor,
        UserId = request.Trans.UserId
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