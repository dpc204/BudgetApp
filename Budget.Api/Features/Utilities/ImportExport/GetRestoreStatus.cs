using Budget.Api.Services;

namespace Budget.Api.Features.Utilities.ImportExport;

/// <summary>
/// Gets the status of a restore job, including the detailed progress log
/// </summary>
public static class GetRestoreStatus
{
  public sealed record Query(string RestoreId) : IRequest<Response?>;

  public sealed record Response(
    string RestoreId,
    DateTime StartTime,
    DateTime? EndTime,
    int TotalTables,
    int CompletedTables,
    int FailedTables,
    bool IsComplete,
    string? ErrorMessage,
    IReadOnlyList<string> LogMessages);

  /// <summary>
  /// Handles restore status retrieval
  /// </summary>
  public class Handler(IRestoreProgressService progressService) : IRequestHandler<Query, Response?>
  {
    public Task<Response?> Handle(Query request, CancellationToken cancellationToken)
    {
      var status = progressService.GetStatus(request.RestoreId);
      if(status == null)
        return Task.FromResult<Response?>(null);

      return Task.FromResult<Response?>(new Response(
        status.RestoreId,
        status.StartTime,
        status.EndTime,
        status.TotalTables,
        status.CompletedTables,
        status.FailedTables,
        status.IsComplete,
        status.ErrorMessage,
        status.LogMessages));
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/utilities/restore-status/{restoreId}", async (
        [FromRoute] string restoreId,
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query(restoreId));
        return result != null ? Results.Ok(result) : Results.NotFound();
      }).RequireAuthorization("Admin");
    }
  }
}
