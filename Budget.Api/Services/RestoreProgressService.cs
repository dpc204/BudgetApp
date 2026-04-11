using System.Collections.Concurrent;

namespace Budget.Api.Services;

/// <summary>
/// In-memory implementation of IRestoreProgressService
/// </summary>
public class RestoreProgressService : IRestoreProgressService
{
  private readonly ConcurrentDictionary<string, RestoreState> _restores = new();

  public string StartRestore()
  {
    var id = Guid.NewGuid().ToString();
    _restores[id] = new RestoreState { RestoreId = id, StartTime = DateTime.UtcNow };
    return id;
  }

  public void AppendLog(string restoreId, string message)
  {
    if(_restores.TryGetValue(restoreId, out var state))
      state.AddLog($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
  }

  public void SetTotal(string restoreId, int totalTables)
  {
    if(_restores.TryGetValue(restoreId, out var state))
    {
      lock(state)
        state.TotalTables = totalTables;
    }
  }

  public void IncrementCompleted(string restoreId)
  {
    if(_restores.TryGetValue(restoreId, out var state))
    {
      lock(state)
        state.CompletedTables++;
    }
  }

  public void IncrementFailed(string restoreId)
  {
    if(_restores.TryGetValue(restoreId, out var state))
    {
      lock(state)
        state.FailedTables++;
    }
  }

  public void Complete(string restoreId)
  {
    if(_restores.TryGetValue(restoreId, out var state))
    {
      lock(state)
      {
        state.EndTime = DateTime.UtcNow;
        state.IsComplete = true;
      }
    }
  }

  public void Fail(string restoreId, string errorMessage)
  {
    if(_restores.TryGetValue(restoreId, out var state))
    {
      lock(state)
      {
        state.EndTime = DateTime.UtcNow;
        state.IsComplete = true;
        state.ErrorMessage = errorMessage;
      }
    }
  }

  public RestoreStatus? GetStatus(string restoreId)
  {
    return _restores.TryGetValue(restoreId, out var state) ? state.ToStatus() : null;
  }

  private sealed class RestoreState
  {
    public string RestoreId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int TotalTables { get; set; }
    public int CompletedTables { get; set; }
    public int FailedTables { get; set; }
    public bool IsComplete { get; set; }
    public string? ErrorMessage { get; set; }

    private readonly List<string> _logMessages = [];

    public void AddLog(string message)
    {
      lock(_logMessages)
        _logMessages.Add(message);
    }

    public RestoreStatus ToStatus()
    {
      IReadOnlyList<string> snapshot;
      lock(_logMessages)
        snapshot = [.. _logMessages];

      lock(this)
      {
        return new RestoreStatus(
          RestoreId,
          StartTime,
          EndTime,
          TotalTables,
          CompletedTables,
          FailedTables,
          IsComplete,
          ErrorMessage,
          snapshot);
      }
    }
  }
}
