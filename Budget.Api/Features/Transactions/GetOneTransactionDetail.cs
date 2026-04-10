//using Budget.Shared.Services;

namespace Budget.Api.Features.Transactions;

public static class GetOneTransactionDetail
{
  public sealed record Query(int TransactionId) : IRequest<Response?>;

  public sealed class Response
  {
    public int Id { get; set; }
    public int AccountId { get; set; }
    public DateTime Date { get; set; }
    public required string Vendor { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string UserInitials { get; set; } = string.Empty;
    public bool IsVoided { get; set; }
    public List<TransactionDetailDto> Details { get; set; } = [];
  }

  public class Handler(BudgetContext db) : IRequestHandler<Query, Response?>
  {
    public async Task<Response?> Handle(Query request, CancellationToken cancellationToken)
    {
      var result = await db.Transactions
        .Include(t => t.User)
        .Include(t => t.Details)
          .ThenInclude(d => d.Envelope) // ensure Envelope is loaded per detail (EnvelopeId FK)
        .Where(t => t.Id == request.TransactionId)
        .Select(t => new Response {
          Id = t.Id,
          AccountId = t.AccountId,
          Date = t.Date,
          Vendor = t.Vendor,
          Description = t.Description,
          TotalAmount = t.TotalAmount,
#pragma warning disable CA1845 // Use span-based 'string.Concat' - cannot use spans in expression trees
          UserInitials = t.User.FirstName.Substring(0, 1) + t.User.LastName.Substring(0, 1),
#pragma warning restore CA1845
          IsVoided = t.IsVoided,
          Details = t.Details
            .OrderBy(d => d.LineId)
            .Select(d => new TransactionDetailDto {
              TransactionId = d.TransactionId,
              LineId = d.LineId,
              EnvelopeId = d.EnvelopeId,
              Notes = d.Notes,
              Amount = d.Amount
            })
            .ToList()
        })
        .FirstOrDefaultAsync(cancellationToken);

      return result; // may be null
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/transactions/detail/{transactionId:int}", async ([FromServices] ISender sender, int transactionId) =>
      {
        var result = await sender.Send(new Query(transactionId));
        return result is null ? Results.NotFound() : Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}