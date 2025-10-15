using Budget.DB;
using Budget.Shared.Models;
//using Budget.Shared.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.Transactions;

public static class GetOneTransactionDetail
{
  public sealed record Query(int TransactionId) : IRequest<Response?>;

  public sealed class Response
  {
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Vendor { get; set; }
      
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string UserInitials { get; set; } = string.Empty;
    public decimal BalanceAfterTransaction { get; set; }
    public List<TransactionDto> Details { get; set; } = [];
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
        .Select(t => new Response
        {
          Id = t.Id,
          Date = t.Date,
          Vendor = t.Vendor,
          TotalAmount = t.TotalAmount,
          UserInitials = (t.User.FirstName.Substring(0,1) + t.User.LastName.Substring(0,1)),
          BalanceAfterTransaction = t.BalanceAfterTransaction,
          Details = t.Details
            .OrderBy(d => d.LineId)
            .Select(d => new TransactionDto
            {
              TransactionId = d.TransactionId,
              LineId = d.LineId,
              Description = d.Notes,
              Amount = d.Amount,
              Date = t.Date,
              EnvelopeId = d.Envelope.Id,
              EnvelopeName = d.Envelope.Name
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
      });
    }
  }
}