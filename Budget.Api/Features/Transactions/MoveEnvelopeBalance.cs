using Carter;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Transactions;

public interface IMoveEnvelopeBalance
{
  public Task MoveBalance(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove);
}
public class MoveEnvelopeBalance : IMoveEnvelopeBalance
{
  public async Task MoveBalance(BudgetContext db, int fromEnvelopeId, int toEnvelopeId, decimal amountToMove)
  {
    var fromEnvelope = await db.Envelopes.FirstOrDefaultAsync(e => e.Id == fromEnvelopeId);
    var toEnvelope = await db.Envelopes.FirstOrDefaultAsync(e => e.Id == toEnvelopeId);

    if(fromEnvelope == null || toEnvelope == null)
      throw new InvalidOperationException("One or both envelopes do not exist.");

    //if (fromEnvelope.Balance < amountToMove)
    //  throw new InvalidOperationException("Insufficient balance in the source envelope.");

    toEnvelope.Balance += amountToMove;
    fromEnvelope.Balance -= amountToMove;

    await db.SaveChangesAsync();
  }

  /// <summary>
  /// Carter endpoint that exposes the envelope transfer operation over HTTP
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/envelopes/transfer", async (
        [FromBody] TransferRequest request,
        [FromServices] IMoveEnvelopeBalance service,
        BudgetContext db) =>
      {
        try
        {
          await service.MoveBalance(db, request.FromEnvelopeId, request.ToEnvelopeId, request.Amount);
          return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(ex.Message);
        }
      }).RequireAuthorization();
    }
  }

  /// <summary>
  /// Request body for envelope transfer
  /// </summary>
  public sealed record TransferRequest(int FromEnvelopeId, int ToEnvelopeId, decimal Amount);
}