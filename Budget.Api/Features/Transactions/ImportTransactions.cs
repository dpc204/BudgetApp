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
  public class Handler(BudgetContext db, ICurrentFamilyService currentFamilyService) : IRequestHandler<Command, int>
  {
    public async Task<int> Handle(Command request, CancellationToken cancellationToken)
    {
      var familyId = currentFamilyService.GetCurrentFamilyId();

      var entities = request.Transactions.Select(dto => new TransactionImport
      {
        Date = dto.Date,
        Vendor = dto.Vendor,
        Description = dto.Description,
        Amount = dto.Amount,
        EnvelopeId = dto.EnvelopeId,
        EnvelopeName = dto.EnvelopeName,
        UserId = dto.UserId,
        FamilyId = familyId,
        ImportedAt = DateTime.UtcNow,
        Duplicate = false
      }).ToList();

      SetVendor(entities);


      db.TransactionImports.AddRange(entities);
      await db.SaveChangesAsync(cancellationToken);

      // Detect duplicates by comparing with existing transactions
      await DetectDuplicatesAsync(entities, cancellationToken);

      return entities.Count;
    }

    private static void SetVendor(List<TransactionImport> entities)
    {
      foreach (var dto in entities)
      {
        if (!string.IsNullOrWhiteSpace(dto.Vendor))
          continue;

        var idx = dto.Description.IndexOf(' ');
        if (idx < 6 && dto.Description.Length > 10)
          idx = dto.Description.IndexOf(' ', idx + 1);


        if (idx == -1)
        {
          dto.Vendor = dto.Description;
          dto.Description = string.Empty;
        }
        else
        {
          dto.Vendor = dto.Description.Substring(0, idx).Trim();
          dto.Description = dto.Description.Substring(idx + 1).Trim();
        }
      }
    }

    private async Task DetectDuplicatesAsync(List<TransactionImport> imports, CancellationToken cancellationToken)
    {
      // Get all existing transactions for the family to compare
      var existingTransactions = await db.Transactions
        .Where(t => !t.IsVoided)
        .Select(t => new { t.Date, t.Vendor, t.TotalAmount })
        .ToListAsync(cancellationToken);

      // Mark imports as duplicates if they match existing transactions
      foreach (var import in imports)
      {
        var isDuplicate = existingTransactions.Any(t =>
          t.Date.Date == import.Date.Date &&
          t.Vendor.Equals(import.Vendor, StringComparison.OrdinalIgnoreCase) &&
          t.TotalAmount == import.Amount);

        if (isDuplicate)
        {
          import.Duplicate = true;
        }
      }

      await db.SaveChangesAsync(cancellationToken);
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