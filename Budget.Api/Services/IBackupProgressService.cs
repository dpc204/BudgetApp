namespace Budget.Api.Services;

/// <summary>
/// Service to track backup progress for background jobs
/// </summary>
public interface IBackupProgressService
{
  /// <summary>
  /// Starts tracking a new backup job
  /// </summary>
  string StartBackup();

  /// <summary>
  /// Updates the progress of a backup job
  /// </summary>
  void UpdateProgress(string backupId, int totalTables, int completedTables, int failedTables, string? currentTable = null, string? errorMessage = null);

  /// <summary>
  /// Marks a backup job as completed
  /// </summary>
  void CompleteBackup(string backupId, int totalTables, int completedTables, int failedTables);

  /// <summary>
  /// Gets the current status of a backup job
  /// </summary>
  BackupStatus? GetStatus(string backupId);
}

/// <summary>
/// Represents the status of a backup job
/// </summary>
public sealed record BackupStatus(
  string BackupId,
  DateTime StartTime,
  DateTime? EndTime,
  int TotalTables,
  int CompletedTables,
  int FailedTables,
  string? CurrentTable,
  string? ErrorMessage,
  bool IsComplete);
