using Budget.Api.Shared.Extensions;
using Budget.Shared.Models.Queries;

namespace Budget.Api.Features.Transactions;

public static class GetUnassignedVirtual
{
  public sealed record Query(AssignQuery assignQuery) : IRequest<Result<Response>>;
  public sealed record Response(AssignQueryResult assignResult);

  public class Handler(BudgetContext db) : IRequestHandler<Query, Result<Response>>
  {
    public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
      var unassignedEnvelope = await GetEnvelopeByType.Get(db, EnvelopeTypes.Unassigned, cancellationToken);

      if (unassignedEnvelope is null)
        return Result.FailIf(unassignedEnvelope == null, "System Error: UnassignedEnvelope not defined");


      var query = (from td in db.TransactionDetails
        join t in db.Transactions on td.TransactionId equals t.Id
        join e in db.Envelopes on td.EnvelopeId equals e.Id
        where td.EnvelopeId == unassignedEnvelope.Id
        select new TransactionDto
        {
          TransactionId = t.Id,
          LineId = td.LineId,
          EnvelopeId = e.Id,
          EnvelopeName = e.Name,
          Vendor = t.Vendor,
          Description = td.Notes,
          Amount = td.Amount,
          Date = t.Date
        }).AsNoTracking();


      query = query.ApplyFilters(request.assignQuery.Filters);

      if (!string.IsNullOrEmpty(request.assignQuery.Sort))
      {
        query = request.assignQuery.Descending
          ? query.OrderByDescendingDynamic(request.assignQuery.Sort)
          : query.OrderByDynamic(request.assignQuery.Sort);
      }

      var totalCount = await query.CountAsync(cancellationToken);
      
      query = query
        .Skip(request.assignQuery.StartIndex)
        .Take(request.assignQuery.Count);

      var items = await query
        .ToListAsync(cancellationToken);

      //return Result.Ok<IEnumerable<Response>>(result);

      var result = new AssignQueryResult
      {
        Items =  items,
        TotalCount = totalCount
      };

      return Result.Ok(new Response(result));
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("transactions/unassigned/virtual", async ([FromServices] ISender sender, [FromBody] AssignQuery assignQuery) =>
      {
        var result = await sender.Send(new Query(assignQuery));
        return result.IsSuccess
          ? Results.Ok(result.Value.assignResult)
          : Results.BadRequest(result.Errors);
      }).RequireAuthorization();
    }
  }
}