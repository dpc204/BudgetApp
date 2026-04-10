using Microsoft.SqlServer.Dac;

namespace Budget.Api.Features.Utilities.Backup;

/// <summary>
/// Download a database backup as a .bacpac file
/// </summary>
public static class DownloadBackup
{
  public sealed record Query(string? Name) : IRequest<IResult>;

  /// <summary>
  /// Handles generating and streaming the database backup file
  /// </summary>
  public class Handler(BudgetContext db, ILogger<Handler> logger) : IRequestHandler<Query, IResult>
  {
    public async Task<IResult> Handle(Query request, CancellationToken cancellationToken)
    {
      var conn = db.Database.GetDbConnection();
      var connString = conn.ConnectionString;
      var databaseName = conn.Database;

      // Optional name from query so client can pre-display the exact filename
      var fileName = !string.IsNullOrWhiteSpace(request.Name)
        ? request.Name
        : $"{databaseName}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.bacpac";

      var tempPath = Path.Combine(Path.GetTempPath(), fileName);

      try
      {
        var dac = new DacServices(connString);
        logger.LogInformation("Starting DacFx export of {Database} to {File}", databaseName, tempPath);
        await Task.Run(() => dac.ExportBacpac(tempPath, databaseName), cancellationToken);

        if(!File.Exists(tempPath))
        {
          logger.LogError("DacFx reported success but file not found: {File}", tempPath);
          return Results.Problem("Export failed: output file missing.", statusCode: 500);
        }

        logger.LogInformation("Export complete. Streaming {FileName} ({Size} bytes)", fileName,
          new FileInfo(tempPath).Length);

        byte[] fileBytes;
        using(var stream = File.OpenRead(tempPath))
        {
          fileBytes = new byte[stream.Length];
          await stream.ReadExactlyAsync(fileBytes, 0, (int)stream.Length, cancellationToken);
        }

        return Results.File(fileBytes, "application/octet-stream", fileName);
      }
      catch(Exception ex)
      {
        logger.LogError(ex, "Error running DacFx export");
        return Results.Problem(ex.ToString(), statusCode: 500);
      }
      finally
      {
        // Clean up temp file after delay
        _ = Task.Run(async () =>
        {
          try
          {
            await Task.Delay(TimeSpan.FromMinutes(5));
            if(File.Exists(tempPath)) File.Delete(tempPath);
          }
          catch
          {
            // Ignore cleanup errors
          }
        }, CancellationToken.None);
      }
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/api/maintenance/backup-download", async (ISender sender, string? name) =>
      {
        var result = await sender.Send(new Query(name));
        return result;
      })

      .WithName("DownloadBackup")
      .WithTags("Maintenance")
      .RequireAuthorization("Admin");
    }
  }
}
