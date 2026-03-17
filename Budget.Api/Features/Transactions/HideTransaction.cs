namespace Budget.Api.Features.Transactions;

/// <summary>
/// Toggles the hidden-from-assign flag on a transaction
/// </summary>
public static class HideTransaction
{
  public sealed record Command(int TransactionId, bool Hidden) : IRequest<bool>;

  /// <summary>
  /// Handles toggling the TransactionHiddenFromAssign flag
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, bool>
  {
    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
      var transaction = await db.Transactions
        .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

      if (transaction is null)
        return false;

      transaction.TransactionHiddenFromAssign = request.Hidden;
      await db.SaveChangesAsync(cancellationToken);
      return true;
    }
  }

  /// <summary>
  /// Maps the endpoint route
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/transactions/hide", async ([FromServices] ISender sender, Command command) =>
      {
        var result = await sender.Send(command);
        return result ? Results.Ok() : Results.NotFound();
      }).RequireAuthorization();
    }
  }
}
