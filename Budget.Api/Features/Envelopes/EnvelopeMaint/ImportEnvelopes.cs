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
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, request.CsvContent, cancellationToken);

        try
        {
          var envelopes = await CsvImportService.ImportAsync(db.Envelopes, tempFile, log: log);
          await db.SaveChangesAsync(cancellationToken);
          importedCount = envelopes.Count;
        }
        catch (Exception ex)
        {
          errors.Add($"Import failed: {ex.Message}");
        }
        finally
        {
          if (File.Exists(tempFile))
          {
            File.Delete(tempFile);
          }
        }
      }
      catch (Exception ex)
      {
        errors.Add($"File processing failed: {ex.Message}");
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
      });
    }
  }

  public sealed class ImportRequest
  {
    public string CsvContent { get; set; } = string.Empty;
  }
}
