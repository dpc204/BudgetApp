namespace Budget.Api.Features.Envelopes.EnvelopeMaint;

/// <summary>
/// Exports envelopes to CSV format
/// </summary>
public static class ExportEnvelopes
{
  public sealed record Query : IRequest<string>;

  /// <summary>
  /// Handles CSV export of envelopes
  /// </summary>
  public class Handler(BudgetContext db, ILogger<Handler> log) : IRequestHandler<Query, string>
  {
    public async Task<string> Handle(Query request, CancellationToken cancellationToken)
    {
      log.LogInformation("Starting envelope export to CSV");
      
      var envelopes = await db.Envelopes
        .OrderBy(e => e.CategoryId)
        .ThenBy(e => e.SortOrder)
        .ThenBy(e => e.Name)
        .ToListAsync(cancellationToken);
      
      var csv = CsvExportService.ExportToCsv(envelopes, log: log);
      
      log.LogInformation("Exported {Count} envelopes to CSV", envelopes.Count);
      return csv;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/envelopes/maint/export", async ([FromServices] ISender sender) =>
      {
        var csv = await sender.Send(new Query());
        return Results.Text(csv, "text/csv");
      });
    }
  }
}
