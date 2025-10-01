using Budget.DB;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Accounts.AccountMaint;

public static class UpdateAccount
{
  public sealed record Command(int Id, string Name, decimal Balance, BankAccount.AccountTypes AccountType) : IRequest<Response?>;
  public sealed record Response(int Id, string Name, decimal Balance, BankAccount.AccountTypes AccountType);

  public class Handler(BudgetContext db) : IRequestHandler<Command, Response?>
  {
    public async Task<Response?> Handle(Command request, CancellationToken cancellationToken)
    {
      var entity = await db.BankAccounts.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
      if (entity is null) return null;

      entity.Name = request.Name;
      entity.Balance = request.Balance;
      entity.AccountType = request.AccountType;

      await db.SaveChangesAsync(cancellationToken);

      return new Response(entity.Id, entity.Name, entity.Balance, entity.AccountType);
    }
  }

  public sealed class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/accounts/maint/{id}", async (int id, [FromBody] CommandBody body, ISender sender) =>
      {
        if (id != body.Id) return Results.BadRequest("Route id and payload id differ.");
        var result = await sender.Send(new Command(body.Id, body.Name, body.Balance, body.AccountType));
        return result is null ? Results.NotFound() : Results.Ok(result);
      });
    }
  }

  public sealed class CommandBody
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public BankAccount.AccountTypes AccountType { get; set; }
  }
}
