using Budget.DB;
using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Transactions;

/// <summary>
/// Clear staged transaction imports
/// </summary>
public static class ClearTransactionImports
{
  public sealed record Command : IRequest<int>;

  /// <summary>
  /// Handles clearing all staged transaction imports
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, int>
  {
    public async Task<int> Handle(Command request, CancellationToken cancellationToken)
    {
      var count = await db.TransactionImports.CountAsync(cancellationToken);
      await db.TransactionImports.ExecuteDeleteAsync(cancellationToken);
      return count;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapDelete("/Transaction/Import", async ([FromServices] ISender sender) =>
      {
        var command = new Command();
        var count = await sender.Send(command);
        return Results.Ok(new { Count = count });
      }).RequireAuthorization();
    }
  }
}
