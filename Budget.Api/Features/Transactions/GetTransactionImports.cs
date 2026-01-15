using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Transactions;

/// <summary>
/// Get staged transaction imports
/// </summary>
public static class GetTransactionImports
{
  public sealed record Query : IRequest<List<TransactionImportDto>>;

  public sealed record Response(List<TransactionImportDto> Imports);

  /// <summary> 
  /// Handles retrieval of staged transaction imports
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, List<TransactionImportDto>>
  {
    public async Task<List<TransactionImportDto>> Handle(Query request, CancellationToken cancellationToken)
    {
      var imports = await db.TransactionImports
        .OrderBy(t => t.Date)
        .Select(t => new TransactionImportDto
        {
          Id = t.Id,
          Date = t.Date,
          Vendor = t.Vendor,
          Description = t.Description,
          Amount = t.Amount,
          EnvelopeId = t.EnvelopeId,
          EnvelopeName = t.EnvelopeName,
          UserId = t.UserId,
          ImportedAt = t.ImportedAt,
          Duplicate = t.Duplicate
        })
        .ToListAsync(cancellationToken);

      return imports;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/Transaction/Import", async ([FromServices] ISender sender) =>
      {
        var query = new Query();
        var imports = await sender.Send(query);
        return Results.Ok(imports);
      }).RequireAuthorization();
    }
  }
}
