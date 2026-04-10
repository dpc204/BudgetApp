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
        .ProjectToType<TransactionImportDto>()
        .OrderByDescending(a => a.Date)
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