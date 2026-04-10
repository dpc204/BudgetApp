namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Copies budget or draft data from one month to the next month
/// </summary>
public static class CopyBudgetToNextMonth
{
  public sealed record Command(int SourceAcctPeriod, bool CopyFromDraft, bool ConfirmOverwrite = false)
    : IRequest<Response>;

  public sealed record Response(bool Success, string Message, int RecordsUpdated, bool WouldOverwriteData);

  /// <summary>
  /// Handles copying budget data to the next month
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    /// <summary>
    /// Copies budget or draft values from the specified source accounting period into the following month, creating new target records or updating existing ones where allowed.
    /// </summary>
    /// <returns>
    /// A <see cref="Response"/> indicating operation outcome: `Success` is `true` when values were copied (with `RecordsUpdated` set and `Message` describing the result); `Success` is `false` when the request is rejected — for example, due to an invalid accounting period format or because the target month contains draft data that would be overwritten (in the latter case `WouldOverwriteData` is `true`).
    /// </returns>
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Validate AcctPeriod format
      var sourceYear = request.SourceAcctPeriod / 100;
      var sourceMonth = request.SourceAcctPeriod % 100;

      if(sourceMonth < 1 || sourceMonth > 12 || sourceYear < 1900)
      {
        return new Response(false, "Invalid accounting period format", 0, false);
      }

      // Calculate target month (next month)
      var sourceDate = new DateTime(sourceYear, sourceMonth, 1);
      var targetDate = sourceDate.AddMonths(1);
      var targetAcctPeriod = targetDate.Year * 100 + targetDate.Month;

      // Check if target month has any draft data
      var targetDrafts = await db.BudgetMonths
        .AsNoTracking()
        .Where(b => b.AcctPeriod == targetAcctPeriod && b.BudgetDraft != null)
        .AnyAsync(cancellationToken);

      // If there's data to overwrite and user hasn't confirmed, return warning
      if(targetDrafts && !request.ConfirmOverwrite)
      {
        return new Response(false, "Target month has draft data that would be overwritten", 0, true);
      }

      // Get all source month data
      var sourceData = await db.BudgetMonths
        .AsNoTracking()
        .Where(b => b.AcctPeriod == request.SourceAcctPeriod)
        .ToListAsync(cancellationToken);

      // Get existing target month data
      var existingTargetData = await db.BudgetMonths
        .Where(b => b.AcctPeriod == targetAcctPeriod)
        .ToListAsync(cancellationToken);

      int recordsUpdated = 0;

      foreach(var source in sourceData)
      {
        // Determine the value to copy based on CopyFromDraft flag
        decimal? valueToCopy = request.CopyFromDraft ? source.BudgetDraft : source.Budget;

        // Skip null values
        if(valueToCopy == null)
          continue;

        // Find or create target record
        var target = existingTargetData.FirstOrDefault(b => b.EnvelopeId == source.EnvelopeId);

        if(target == null)
        {
          // Create new record in target month with null budget
          target = new BudgetMonth {
            AcctPeriod = targetAcctPeriod,
            EnvelopeId = source.EnvelopeId,
            Budget = null,
            BudgetDraft = valueToCopy
          };
          db.BudgetMonths.Add(target);
        }
        else
        {
          // Update existing record's draft
          if(!target.IsBudgetLocked)
            target.BudgetDraft = valueToCopy ?? 0m;
        }

        recordsUpdated++;
      }

      await db.SaveChangesAsync(cancellationToken);

      var message = request.CopyFromDraft
        ? $"Copied {recordsUpdated} draft values to next month"
        : $"Copied {recordsUpdated} budget values to next month";

      return new Response(true, message, recordsUpdated, false);
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/budgetmonths/copytonextmonth", async (
        [FromServices] ISender sender,
        [FromBody] Command command) =>
      {
        var result = await sender.Send(command);
        return Results.Ok(result);
      }).RequireAuthorization();
    }
  }
}