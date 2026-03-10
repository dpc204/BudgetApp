using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Transactions;

/// <summary>
/// Bulk import transactions to staging table
/// </summary>
public static class ImportTransactionsToStaging
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
        PostingStatus = dto.PostingStatus,
        Vendor = dto.Vendor,
        Description = RemoveConsecutiveSpaces(dto.Description),
        Notes = dto.Notes,
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

      // Detect duplicates by comparing with existing transactions
      await DetectDuplicatesAsync(entities, cancellationToken);
      await CheckForClearedPending(entities, cancellationToken);

      await db.SaveChangesAsync(cancellationToken);
      
      return entities.Count;
    }

    private static string RemoveConsecutiveSpaces(string description)
    {
      if (string.IsNullOrWhiteSpace(description))
        return string.Empty;

      return string.Join(" ", description.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static void SetVendor(List<TransactionImport> entities)
    {
      foreach (var dto in entities)
      {
        if (!string.IsNullOrWhiteSpace(dto.Vendor))
          continue;

        // if dto.Description starts with "POS DEBIT " or "POS CREDIT", remove it from description and set PostingStatus to Pending.  Otherwise, leave description alone and set posting status to Posted
        if (dto.Description.StartsWith("POS DEBIT ", StringComparison.OrdinalIgnoreCase))
        {
          dto.Description = dto.Description[10..]; // Remove "POS DEBIT "
          dto.PostingStatus = PostingStatuses.Pending;
        }
        else if (dto.Description.StartsWith("POS CREDIT ", StringComparison.OrdinalIgnoreCase))
        {
          dto.Description = dto.Description[11..]; // Remove "POS CREDIT "
          dto.PostingStatus = PostingStatuses.Pending;
        }
        else
        {
          dto.PostingStatus = PostingStatuses.Posted;
        }

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
          dto.Vendor = dto.Description[..idx].Trim();
          dto.Description = dto.Description[(idx + 1)..].Trim();
        }
      }
    }

    private async Task CheckForClearedPending(List<TransactionImport> imports, CancellationToken cancellationToken)
    {
      // Get all existing transactions for the family to compare
      var existingTransactions = await db.Transactions
        .Where(t => !t.IsVoided && t.PostingStatus == PostingStatuses.Pending)
        .Select(t => new { t.Date, t.Vendor, t.TotalAmount, t.PostingStatus })
        .ToListAsync(cancellationToken);

      // Mark imports as duplicates if they match existing transactions+
      foreach (var import in imports)
      {
          
        var isClearedPending = existingTransactions.Any(t =>
          t.PostingStatus == PostingStatuses.Pending &&
          Math.Abs((t.Date - import.Date).Days) < 8 &&
          t.Vendor.Equals(import.Vendor, StringComparison.OrdinalIgnoreCase) &&
          t.TotalAmount == import.Amount);
        // how am I supposed to handle splitting the tips when they come after the initial transaction how are tips handled?
        // Do oa comparison of an export from Transactions with the Cvs file
        
        if (isClearedPending)
        {
          import.Duplicate = false;
          import.PostingStatus = PostingStatuses.ToBeCleared;
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