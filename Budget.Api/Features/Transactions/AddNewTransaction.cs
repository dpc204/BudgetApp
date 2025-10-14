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
      var trans = new Transaction() {
        Date = request.Trans.Date,
        Vendor = request.Trans.Vendor,
        TotalAmount = request.Trans.TotalAmount,
        UserId = request.Trans.UserId
      };

      foreach(var detail in request.Trans.Details)
      {
        var dtl = new TransactionDetail() {
          Amount = detail.Amount,
          EnvelopeId = detail.EnvelopeId,
          Notes = detail.Description
        };

        trans.Details.Add(dtl);
      }

      db.Transactions.Add(trans);
      await db.SaveChangesAsync(cancellationToken);
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