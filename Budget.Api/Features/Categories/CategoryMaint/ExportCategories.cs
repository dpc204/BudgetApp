using Budget.Shared.Services;

namespace Budget.Api.Features.Categories.CategoryMaint;

/// <summary>
/// Exports categories to CSV format
/// </summary>
public static class ExportCategories
{
  public sealed record Query : IRequest<string>;

  /// <summary>
  /// Handles CSV export of categories
  /// </summary>
  public class Handler(BudgetContext db, ILogger<Handler> log) : IRequestHandler<Query, string>
  {
    public async Task<string> Handle(Query request, CancellationToken cancellationToken)
    {
      log.LogInformation("Starting category export to CSV");
      
      var categories = await db.Categories
        .OrderBy(c => c.SortOrder)
        .ThenBy(c => c.Name)
        .ToListAsync(cancellationToken);
      
      var csv = CsvExportService.ExportToCsv(categories, log: log);
      
      log.LogInformation("Exported {Count} categories to CSV", categories.Count);
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
      app.MapGet("/categories/maint/export", async ([FromServices] ISender sender) =>
      {
        var csv = await sender.Send(new Query());
        return Results.Text(csv, "text/csv");
      }).RequireAuthorization();
    }
  }
}
