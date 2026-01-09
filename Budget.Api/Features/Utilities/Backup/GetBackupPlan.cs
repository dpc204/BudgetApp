namespace Budget.Api.Features.Utilities.Backup;

/// <summary>
/// Get the filename that will be used for the next database backup
/// </summary>
public static class GetBackupPlan
{
  public sealed record Query : IRequest<Response>;

  public sealed record Response(string FileName);

  /// <summary>
  /// Handles getting the backup plan with the computed filename
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      var conn = db.Database.GetDbConnection();
      var databaseName = conn.Database;
      var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
      var fileName = $"{databaseName}-{stamp}.bacpac";
      
      return await Task.FromResult(new Response(fileName));
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/api/maintenance/backup-plan", async (ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      })
      .WithName("GetBackupPlan")
      .WithTags("Maintenance")
      .RequireAuthorization();
    }
  }
}
