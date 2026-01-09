namespace Budget.Api.Features.Envelopes.EnvelopeMaint;

/// <summary>
/// Imports envelopes from CSV data
/// </summary>
public static class ImportEnvelopes
{
  public sealed record Command(string CsvContent) : IRequest<Response>;
  public sealed record Response(int ImportedCount, List<string> Errors);

  /// <summary>
  /// Handles CSV import of envelopes
  /// </summary>
  public class Handler(BudgetContext db, ILogger<Handler> log) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      var errors = new List<string>();
      var importedCount = 0;
      log.LogInformation("Starting envelope import from CSV");
      try
      {
        // Split the CSV content into lines
        var lines = request.CsvContent
          .Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
          .ToList();
        
        // Enable IDENTITY_INSERT to allow explicit Id values

        if(db.Database.IsSqlServer())
        {
     //     await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Envelopes ON", cancellationToken);
          try
          {
            var envelopes = await CsvImportService.ImportAsync(db.Envelopes, lines, log: log);
            await db.SaveChangesAsync(cancellationToken);
            importedCount = envelopes.Count;
          }
          finally
          {
            // Always disable IDENTITY_INSERT
        //    await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Envelopes OFF", cancellationToken);
          }
        }
        else
        {
          var envelopes = await CsvImportService.ImportAsync(db.Envelopes, lines, log: log);
          await db.SaveChangesAsync(cancellationToken);
          importedCount = envelopes.Count;

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
      app.MapPost("/envelopes/maint/import", async ([FromServices] ISender sender, [FromBody] ImportRequest request) =>
      {
        var result = await sender.Send(new Command(request.CsvContent));
        
        if (result.Errors.Count > 0)
        {
          return Results.BadRequest(result);
        }

        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }

  public sealed class ImportRequest
  {
    public string CsvContent { get; set; } = string.Empty;
  }
}
