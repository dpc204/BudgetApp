using System.Collections.Concurrent;

namespace Budget.Api.Services;

/// <summary>
/// In-memory implementation of IBackupProgressService
/// </summary>
public class BackupProgressService : IBackupProgressService
{
  private readonly ConcurrentDictionary<string, BackupStatus> _backups = new();

  public string StartBackup()
  {
    var backupId = Guid.NewGuid().ToString();
    var status = new BackupStatus(
      BackupId: backupId,
      StartTime: DateTime.UtcNow,
      EndTime: null,
      TotalTables: 0,
      CompletedTables: 0,
      FailedTables: 0,
      CurrentTable: null,
      ErrorMessage: null,
      IsComplete: false);
    
    _backups[backupId] = status;
    return backupId;
  }

  public void UpdateProgress(string backupId, int totalTables, int completedTables, int failedTables, string? currentTable = null, string? errorMessage = null)
  {
    if (_backups.TryGetValue(backupId, out var existing))
    {
      var updated = existing with
      {
        TotalTables = totalTables,
        CompletedTables = completedTables,
        FailedTables = failedTables,
        CurrentTable = currentTable,
        ErrorMessage = errorMessage
      };
      _backups[backupId] = updated;
    }
  }

  public void CompleteBackup(string backupId, int totalTables, int completedTables, int failedTables)
  {
    if (_backups.TryGetValue(backupId, out var existing))
    {
      var updated = existing with
      {
        TotalTables = totalTables,
        CompletedTables = completedTables,
        FailedTables = failedTables,
        EndTime = DateTime.UtcNow,
        CurrentTable = null,
        IsComplete = true
      };
      _backups[backupId] = updated;
    }
  }

  public BackupStatus? GetStatus(string backupId)
  {
    return _backups.TryGetValue(backupId, out var status) ? status : null;
  }
}
