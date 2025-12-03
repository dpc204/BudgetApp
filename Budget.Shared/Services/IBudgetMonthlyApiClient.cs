using Budget.Shared.Enums;

namespace Budget.Shared.Services;

/// <summary>
/// API client for budget monthly operations
/// </summary>
public interface IBudgetMonthlyApiClient
{
  /// <summary>
  /// Gets budget data for a specific month, ensuring all envelopes are represented
  /// </summary>
  Task<IEnumerable<BudgetMonthResponse>> GetBudgetMonthAsync(int year, int month, CancellationToken cancellationToken = default);

  /// <summary>
  /// Checks if there are any draft budget values in the system
  /// </summary>
  Task<CheckDraftsResponse> CheckDraftBudgetsAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Updates the draft budget value for a specific envelope in a specific month
  /// </summary>
  Task<UpdateDraftResponse> UpdateBudgetDraftAsync(int acctPeriod, int envelopeId, decimal? draftValue, CancellationToken cancellationToken = default);

  /// <summary>
  /// Clears all draft budget values for current and future months
  /// </summary>
  Task<ClearDraftsResponse> ClearDraftBudgetsAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Applies all draft budget values to actual budget values
  /// <summary>
/// Applies all draft budget values to the actual budget values for the current and future months.
/// </summary>
/// <returns>An ApplyDraftsResponse containing whether the operation succeeded, a message, and the number of records updated.</returns>
  Task<ApplyDraftsResponse> ApplyDraftBudgetsAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Copies budget or draft data from one month to the next month
  /// <summary>
/// Copies budget data from the specified accounting period to the subsequent month.
/// </summary>
/// <param name="sourceAcctPeriod">Accounting period identifier whose budget data will be copied.</param>
/// <param name="copyFromDraft">If `true`, copy draft budget values; otherwise copy applied (actual) budget values.</param>
/// <returns>A response containing operation success, an informational message, the number of records updated, and whether the operation would overwrite existing data.</returns>
  Task<CopyBudgetToNextMonthResponse> CopyBudgetToNextMonthAsync(int sourceAcctPeriod, bool copyFromDraft, CancellationToken cancellationToken = default);

  /// <summary>
  /// Copies budget or draft data from one month to the next month with confirmation
  /// <summary>
/// Copies budget values from the specified accounting period to the next month, using draft values if requested and respecting the overwrite confirmation flag.
/// </summary>
/// <param name="sourceAcctPeriod">Accounting period identifier to copy from.</param>
/// <param name="copyFromDraft">If true, copy draft budget values; otherwise copy actual budget values.</param>
/// <param name="confirmOverwrite">If true, perform the copy even if it will overwrite existing data; if false, do not overwrite existing data.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>A response containing success status, a message, the number of records updated, and whether the operation would overwrite existing data.</returns>
  Task<CopyBudgetToNextMonthResponse> CopyBudgetToNextMonthAsync(int sourceAcctPeriod, bool copyFromDraft, bool confirmOverwrite, CancellationToken cancellationToken = default);
}

/// <summary>
/// Response for GetBudgetMonth endpoint
/// </summary>
public record BudgetMonthResponse(
  int AcctPeriod,
  int EnvelopeId,
  string EnvelopeName,
  int CategoryId,
  string CategoryName,
  CatTypes CategoryType,
  int SortOrder,
  decimal? Budget,
  decimal? BudgetDraft);

/// <summary>
/// Response for CheckDraftBudgets endpoint
/// </summary>
public record CheckDraftsResponse(bool HasDrafts, int DraftCount);

/// <summary>
/// Response for UpdateBudgetDraft endpoint
/// </summary>
public record UpdateDraftResponse(bool Success, string Message);

/// <summary>
/// Response for ClearDraftBudgets endpoint
/// </summary>
public record ClearDraftsResponse(bool Success, string Message, int RecordsUpdated);

/// <summary>
/// Response for ApplyDraftBudgets endpoint
/// </summary>
public record ApplyDraftsResponse(bool Success, string Message, int RecordsUpdated);

/// <summary>
/// Response for CopyBudgetToNextMonth endpoint
/// </summary>
public record CopyBudgetToNextMonthResponse(bool Success, string Message, int RecordsUpdated, bool WouldOverwriteData);