namespace Budget.Api.Features.Transactions;

/// <summary>
/// Loads transaction imports from TransactionImports table to Transactions and TransactionDetails tables
/// </summary>
public static class LoadTransactionImportsToUnassigned
{
  /// <summary>
  /// Command to load transaction imports
  /// </summary>
  public sealed record Command(int AccountId, int UserId) : IRequest<Response>;

  /// <summary>
  /// Response containing the count of imported transactions
  /// </summary>
  public sealed record Response(int ImportedCount);

  /// <summary>
  /// Handles loading transaction imports to the Unassigned envelope
  /// </summary>
  public class Handler(BudgetContext db, ISender sender) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Get the Unassigned envelope
      var unassigned = await Shared.GetEnvelopeByType.Get(db, EnvelopeTypes.Unassigned, cancellationToken) ?? throw new InvalidOperationException("Unassigned envelope not found");

      // Get non-duplicate transaction imports
      var nonDuplicates = await db.TransactionImports
        .Where(ti => !ti.Duplicate || ti.KeepDuplicate)
        .ToListAsync(cancellationToken);

      if (nonDuplicates.Count == 0)
      {
        return new Response(0);
      }


      List<OneTransactionDetail> transactionsToAdd = [];

      // Process each transaction import
      foreach (var rec in nonDuplicates)
      {
        var trans = new OneTransactionDetail
        {
          AccountId = request.AccountId,
          Date = rec.Date,
          Vendor = rec.Vendor,
          Description = rec.Description,
          UserId = request.UserId,
          UserName = string.Empty,
          WasPotentialDuplicate = rec.KeepDuplicate,
          Details =
          [
            new TransactionDetailDto
            {
              TransactionId = 0, // Will be set by backend
              LineId = 0, // Will be set by backend
              EnvelopeId = unassigned.Id,
              Amount = rec.Amount,
              Notes = rec.Notes
            }
          ]
        };

        transactionsToAdd.Add(trans);
      }

      await sender.Send(new AddMultipleTransaction.Command(transactionsToAdd), cancellationToken);
      // Clear the staging table after successful import
      var importedCount = nonDuplicates.Count;
      db.TransactionImports.RemoveRange(nonDuplicates);
      await db.SaveChangesAsync(cancellationToken);

      return new Response(importedCount);
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/api/transactions/load-imports", async ([FromServices] ISender sender, [FromBody] Command command) =>
      {
        var result = await sender.Send(command);
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}