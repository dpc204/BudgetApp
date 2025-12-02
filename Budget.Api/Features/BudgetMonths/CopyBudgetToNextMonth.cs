namespace Budget.Api.Features.BudgetMonths;

/// <summary>
/// Copies budget or draft data from one month to the next month
/// </summary>
public static class CopyBudgetToNextMonth
{
  public sealed record Command(int SourceAcctPeriod, bool CopyFromDraft) : IRequest<Response>;
  
  public sealed record Response(bool Success, string Message, int RecordsUpdated, bool HasOverwrittenData);

  /// <summary>
  /// Handles copying budget data to the next month
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Calculate target month (next month)
      var sourceYear = request.SourceAcctPeriod / 100;
      var sourceMonth = request.SourceAcctPeriod % 100;
      var sourceDate = new DateTime(sourceYear, sourceMonth, 1);
      var targetDate = sourceDate.AddMonths(1);
      var targetAcctPeriod = targetDate.Year * 100 + targetDate.Month;

      // Get all source month data
      var sourceData = await db.BudgetMonths
        .AsNoTracking()
        .Where(b => b.AcctPeriod == request.SourceAcctPeriod)
        .ToListAsync(cancellationToken);

      // Check if target month has any draft data (for confirmation)
      var targetDrafts = await db.BudgetMonths
        .AsNoTracking()
        .Where(b => b.AcctPeriod == targetAcctPeriod && b.BudgetDraft != null)
        .AnyAsync(cancellationToken);

      // Get existing target month data
      var existingTargetData = await db.BudgetMonths
        .Where(b => b.AcctPeriod == targetAcctPeriod)
        .ToListAsync(cancellationToken);

      int recordsUpdated = 0;

      foreach (var source in sourceData)
      {
        // Determine the value to copy based on CopyFromDraft flag
        decimal? valueToCopy = request.CopyFromDraft ? source.BudgetDraft : source.Budget;

        // Skip null values
        if (valueToCopy == null)
          continue;

        // Find or create target record
        var target = existingTargetData.FirstOrDefault(b => b.EnvelopeId == source.EnvelopeId);

        if (target == null)
        {
          // Create new record in target month
          target = new BudgetMonth
          {
            AcctPeriod = targetAcctPeriod,
            EnvelopeId = source.EnvelopeId,
            Budget = 0,
            BudgetDraft = valueToCopy
          };
          db.BudgetMonths.Add(target);
        }
        else
        {
          // Update existing record's draft
          target.BudgetDraft = valueToCopy;
        }

        recordsUpdated++;
      }

      await db.SaveChangesAsync(cancellationToken);

      var message = request.CopyFromDraft
        ? $"Copied {recordsUpdated} draft values to next month"
        : $"Copied {recordsUpdated} budget values to next month";

      return new Response(true, message, recordsUpdated, targetDrafts);
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
      });
    }
  }
}
