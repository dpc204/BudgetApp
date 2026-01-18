using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Transactions;

/// <summary>
/// Batch update transaction import records (e.g., toggle Duplicate flag for multiple records)
/// </summary>
public static class UpdateTransactionImportsBatch
{
  public sealed record Command(List<int> Ids, bool Duplicate) : IRequest<int>;

  /// <summary>
  /// Handles batch updating transaction import records
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, int>
  {
    public async Task<int> Handle(Command request, CancellationToken cancellationToken)
    {
      if (request.Ids.Count == 0)
      {
        return 0;
      }

      var imports = await db.TransactionImports
        .Where(t => request.Ids.Contains(t.Id))
        .ToListAsync(cancellationToken);

      foreach (var import in imports)
      {
        import.Duplicate = request.Duplicate;
      }

      await db.SaveChangesAsync(cancellationToken);

      return imports.Count;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/Transaction/Import/Batch", async ([FromServices] ISender sender, [FromBody] BatchUpdateRequest request) =>
      {
        var command = new Command(request.Ids, request.Duplicate);
        var count = await sender.Send(command);
        return Results.Ok(new { UpdatedCount = count });
      }).RequireAuthorization();
    }
  }

  public record BatchUpdateRequest(List<int> Ids, bool Duplicate);
}
