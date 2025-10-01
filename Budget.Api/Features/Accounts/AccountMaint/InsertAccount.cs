using Budget.DB;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Accounts.AccountMaint;

public static class InsertAccount
{
  public sealed record Command(string Name, decimal Balance, BankAccount.AccountTypes AccountType) : IRequest<Response>;
  public sealed record Response(int Id, string Name, decimal Balance, BankAccount.AccountTypes AccountType);

  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      var acct = new BankAccount
      {
        Name = request.Name,
        Balance = request.Balance,
        AccountType = request.AccountType
      };

      db.BankAccounts.Add(acct);
      await db.SaveChangesAsync(cancellationToken);
      return new Response(acct.Id, acct.Name, acct.Balance, acct.AccountType);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/accounts/maint/Insert", async (ISender sender, Command command) =>
      {
        var acct = await sender.Send(command);
        return Results.Created($"accounts/maint/{acct.Id}", acct);
      });
    }
  }
}
