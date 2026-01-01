using Budget.Api.Services;
using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Utilities.ImportExport;

/// <summary>
/// Gets the status of a backup job
/// </summary>
public static class GetBackupStatus
{
  public sealed record Query(string BackupId) : IRequest<Response?>;

  public sealed record Response(
    string BackupId,
    DateTime StartTime,
    DateTime? EndTime,
    int TotalTables,
    int CompletedTables,
    int FailedTables,
    string? CurrentTable,
    string? ErrorMessage,
    bool IsComplete);

  /// <summary>
  /// Handles backup status retrieval
  /// </summary>
  public class Handler(IBackupProgressService progressService) : IRequestHandler<Query, Response?>
  {
    public Task<Response?> Handle(Query request, CancellationToken cancellationToken)
    {
      var status = progressService.GetStatus(request.BackupId);
      
      if (status == null)
        return Task.FromResult<Response?>(null);

      return Task.FromResult<Response?>(new Response(
        status.BackupId,
        status.StartTime,
        status.EndTime,
        status.TotalTables,
        status.CompletedTables,
        status.FailedTables,
        status.CurrentTable,
        status.ErrorMessage,
        status.IsComplete));
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/utilities/backup-status/{backupId}", async (
        [FromRoute] string backupId,
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query(backupId));
        return result != null ? Results.Ok(result) : Results.NotFound();
      })
      .RequireAuthorization("AdminOnly");
    }
  }
}
