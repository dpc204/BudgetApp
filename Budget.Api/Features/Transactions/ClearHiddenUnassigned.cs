namespace Budget.Api.Features.Transactions;

/// <summary>
/// Clears the TransactionHiddenFromAssign flag for all unassigned transactions
/// </summary>
public static class ClearHiddenUnassigned
{
  public sealed record Command : IRequest<int>;

  /// <summary>
  /// Handles clearing the hidden flag for all unassigned transactions
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, int>
  {
    public async Task<int> Handle(Command request, CancellationToken cancellationToken)
    {
      var unassignedEnvelope = await GetEnvelopeByType.Get(db, EnvelopeTypes.Unassigned, cancellationToken);

      if(unassignedEnvelope is null)
        return 0;

      var transactionsToUpdate = await db.Transactions
        .Where(t => t.TransactionHiddenFromAssign &&
                    db.TransactionDetails.Any(td => td.TransactionId == t.Id && td.EnvelopeId == unassignedEnvelope.Id))
        .ToListAsync(cancellationToken);

      foreach(var transaction in transactionsToUpdate)
      {
        transaction.TransactionHiddenFromAssign = false;
      }

      if(transactionsToUpdate.Count > 0)
      {
        await db.SaveChangesAsync(cancellationToken);
      }

      return transactionsToUpdate.Count;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("transactions/clear-hidden-assign", async ([FromServices] ISender sender) =>
      {
        var count = await sender.Send(new Command());
        return Results.Ok(count);
      }).RequireAuthorization();
    }
  }
}
