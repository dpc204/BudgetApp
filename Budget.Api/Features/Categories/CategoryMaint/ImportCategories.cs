namespace Budget.Api.Features.Categories.CategoryMaint;

/// <summary>
/// Imports categories from CSV data
/// </summary>
public static class ImportCategories
{
  public sealed record Command(string CsvContent) : IRequest<Response>;
  public sealed record Response(int ImportedCount, List<string> Errors);

  /// <summary>
  /// Handles CSV import of categories
  /// </summary>
  public class Handler(BudgetContext db, ILogger<Handler> log) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      var errors = new List<string>();
      var importedCount = 0;
      log.LogInformation("Starting category import from CSV");
      try
      {
        // Split the CSV content into lines
        var lines = request.CsvContent
          .Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
          .ToList();

        // Enable IDENTITY_INSERT to allow explicit Id values
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Categories ON", cancellationToken);
        
        try
        {
          var categories = await CsvImportService.ImportAsync(db.Categories, lines, log: log);
          await db.SaveChangesAsync(cancellationToken);
          importedCount = categories.Count;
        }
        finally
        {
          // Always disable IDENTITY_INSERT
          await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Categories OFF", cancellationToken);
        }
      }
      catch (Exception ex)
      {
        errors.Add($"Import failed: {ex.Message}");
      }

      return new Response(importedCount, errors);
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/categories/maint/import", async ([FromServices] ISender sender, [FromBody] ImportRequest request) =>
      {
        var result = await sender.Send(new Command(request.CsvContent));
        
        if (result.Errors.Count > 0)
        {
          return Results.BadRequest(result);
        }

        return Results.Ok(result);
      });
    }
  }

  public sealed class ImportRequest
  {
    public string CsvContent { get; set; } = string.Empty;
  }
}
