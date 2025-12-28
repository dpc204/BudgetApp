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
  /// Updates the lock status for a specific envelope in a specific month
  /// </summary>
  Task<UpdateLockResponse> UpdateBudgetLockAsync(int acctPeriod, int envelopeId, bool isLocked, CancellationToken cancellationToken = default);

  /// <summary>
  /// Clears all draft budget values for current and future months
  /// </summary>
  Task<ClearDraftsResponse> ClearDraftBudgetsAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Applies all draft budget values to actual budget values
  /// </summary>
  Task<ApplyDraftsResponse> ApplyDraftValuesToBudgetAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Copies budget or draft data from one month to the next month
  /// </summary>
  Task<CopyBudgetToNextMonthResponse> CopyBudgetToNextMonthAsync(int sourceAcctPeriod, bool copyFromDraft, CancellationToken cancellationToken = default);

  /// <summary>
  /// Copies budget or draft data from one month to the next month with confirmation
  /// </summary>
  Task<CopyBudgetToNextMonthResponse> CopyBudgetToNextMonthAsync(int sourceAcctPeriod, bool copyFromDraft, bool confirmOverwrite, CancellationToken cancellationToken = default);

  /// <summary>
  /// Clears budget values for a specific month
  /// </summary>
  Task<ClearMonthBudgetsResponse> ClearMonthBudgetsAsync(int acctPeriod, CancellationToken cancellationToken = default);

  /// <summary>
  /// Clears draft values for a specific month
  /// </summary>
  Task<ClearMonthDraftsResponse> ClearMonthDraftsAsync(int acctPeriod, CancellationToken cancellationToken = default);

  /// <summary>
  /// Clears both budget and draft values for a specific month
  /// </summary>
  Task<ClearMonthBothResponse> ClearMonthBothAsync(int acctPeriod, CancellationToken cancellationToken = default);

  /// <summary>
  /// Applies draft values to budget values for a specific month
  /// </summary>
  Task<ApplyMonthDraftsResponse> ApplyMonthDraftsAsync(int acctPeriod, CancellationToken cancellationToken = default);

  /// <summary>
  /// Updates the fund amount for a specific envelope
  /// </summary>
  Task<UpdateFundAmountResponse> UpdateFundAmountAsync(int envelopeId, decimal? fundAmount, CancellationToken cancellationToken = default);
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
  decimal? BudgetDraft,
  bool IsBudgetLocked,
  decimal FundAmount);

/// <summary>
/// Response for CheckDraftBudgets endpoint
/// </summary>
public record CheckDraftsResponse(bool HasDrafts, int DraftCount);

/// <summary>
/// Response for UpdateBudgetDraft endpoint
/// </summary>
public record UpdateDraftResponse(bool Success, string Message);

/// <summary>
/// Response for UpdateBudgetLock endpoint
/// </summary>
public record UpdateLockResponse(bool Success, string Message);

/// <summary>
/// Response for ClearDraftBudgets endpoint
/// </summary>
public record ClearDraftsResponse(bool Success, string Message, int RecordsUpdated);

/// <summary>
/// Response for ApplyDraftValuesToBudget endpoint
/// </summary>
public record ApplyDraftsResponse(bool Success, string Message, int RecordsUpdated);

/// <summary>
/// Response for CopyBudgetToNextMonth endpoint
/// </summary>
public record CopyBudgetToNextMonthResponse(bool Success, string Message, int RecordsUpdated, bool WouldOverwriteData);

/// <summary>
/// Response for ClearMonthBudgets endpoint
/// </summary>
public record ClearMonthBudgetsResponse(bool Success, string Message, int RecordsUpdated);

/// <summary>
/// Response for ClearMonthDrafts endpoint
/// </summary>
public record ClearMonthDraftsResponse(bool Success, string Message, int RecordsUpdated);

/// <summary>
/// Response for ClearMonthBoth endpoint
/// </summary>
public record ClearMonthBothResponse(bool Success, string Message, int RecordsUpdated);

/// <summary>
/// Response for ApplyMonthDrafts endpoint
/// </summary>
public record ApplyMonthDraftsResponse(bool Success, string Message, int RecordsUpdated);

/// <summary>
/// Response for UpdateFundAmount endpoint
/// </summary>
public record UpdateFundAmountResponse(bool Success, string Message);
