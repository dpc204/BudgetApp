namespace Budget.Api.Services;

/// <summary>
/// Service to track restore progress for background jobs, including a detailed log of each step
/// </summary>
public interface IRestoreProgressService
{
  /// <summary>
  /// Starts tracking a new restore job and returns its unique ID
  /// </summary>
  string StartRestore();

  /// <summary>
  /// Appends a timestamped log message to the restore job
  /// </summary>
  void AppendLog(string restoreId, string message);

  /// <summary>
  /// Sets the total number of tables to restore
  /// </summary>
  void SetTotal(string restoreId, int totalTables);

  /// <summary>
  /// Increments the completed table count
  /// </summary>
  void IncrementCompleted(string restoreId);

  /// <summary>
  /// Increments the failed table count
  /// </summary>
  void IncrementFailed(string restoreId);

  /// <summary>
  /// Marks the restore job as successfully completed
  /// </summary>
  void Complete(string restoreId);

  /// <summary>
  /// Marks the restore job as failed with an error message
  /// </summary>
  void Fail(string restoreId, string errorMessage);

  /// <summary>
  /// Gets the current status of a restore job, or null if not found
  /// </summary>
  RestoreStatus? GetStatus(string restoreId);
}

/// <summary>
/// Represents the current status of a restore job
/// </summary>
public sealed record RestoreStatus(
  string RestoreId,
  DateTime StartTime,
  DateTime? EndTime,
  int TotalTables,
  int CompletedTables,
  int FailedTables,
  bool IsComplete,
  string? ErrorMessage,
  IReadOnlyList<string> LogMessages);
