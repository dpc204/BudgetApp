using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Transactions;

/// <summary>
/// Update a transaction import record (e.g., toggle Duplicate flag)
/// </summary>
public static class UpdateTransactionImport
{
  public record UpdateRequest(bool Duplicate, PotentialDuplicates PotentialDuplicate);
  public sealed record Command(int Id, bool Duplicate, PotentialDuplicates PotentialDuplicate) : IRequest<bool>;

  /// <summary>
  /// Handles updating a transaction import record
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, bool>
  {
    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
      var import = await db.TransactionImports
        .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

      if (import == null)
      {
        return false;
      }

      import.Duplicate = request.Duplicate;
      import.PotentialDuplicate = request.PotentialDuplicate;

      await db.SaveChangesAsync(cancellationToken);

      return true;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/Transaction/Import/{id}", async ([FromServices] ISender sender, int id, [FromBody] UpdateRequest request) =>
      {
        var command = new Command(id, request.Duplicate, request.PotentialDuplicate);
        var success = await sender.Send(command);
        return success ? Results.Ok() : Results.NotFound();
      }).RequireAuthorization();
    }
  }


}
