using Budget.DB;
using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Transactions;

/// <summary>
/// Bulk import transactions to staging table
/// </summary>
public static class ImportTransactions
{
  public sealed record Command(List<TransactionImportDto> Transactions) : IRequest<int>;

  /// <summary>
  /// Handles bulk import of transactions to staging table
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, int>
  {
    public async Task<int> Handle(Command request, CancellationToken cancellationToken)
    {
      var entities = request.Transactions.Select(dto => new TransactionImport
      {
        Date = dto.Date,
        Vendor = dto.Vendor,
        Description = dto.Description,
        Amount = dto.Amount,
        EnvelopeId = dto.EnvelopeId,
        EnvelopeName = dto.EnvelopeName,
        UserId = dto.UserId,
        ImportedAt = DateTime.UtcNow
      }).ToList();

      db.TransactionImports.AddRange(entities);
      await db.SaveChangesAsync(cancellationToken);

      return entities.Count;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/Transaction/Import", async ([FromServices] ISender sender, Command command) =>
      {
        var count = await sender.Send(command);
        return Results.Ok(new { Count = count });
      }).RequireAuthorization();
    }
  }
}
