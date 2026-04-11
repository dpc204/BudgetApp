using Budget.Web.Services;

namespace Budget.Client.Components.Maintenance.BackupRestore;

public partial class RestoreFromCsvIndex : IDisposable
{
  [Inject] private IUtilitiesApiClient MaintApiClient { get; set; } = null!;
  [Inject] private ISnackbar Snackbar { get; set; } = null!;
  [Inject] private IDialogService DialogService { get; set; } = null!;
  [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
  [Inject] private ILogger<RestoreFromCsvIndex> Logger { get; set; } = null!;
  [Inject] private DatabaseEnvironmentService BudgetEnvironment { get; set; } = null!;

  private List<BackupSetDto>? _backupSets;
  private List<BackupTableDto>? _backupTables;
  private BackupSetDto? _selectedBackupSet;
  private string _targetDatabase = "azure";
  private bool _isDevelopment;

  private bool IsAdmin { get; set; }
  private bool RestoreBusy { get; set; }

  private string? _currentRestoreId;
  private readonly List<string> _logMessages = [];
  private System.Timers.Timer? _pollTimer;

  private static readonly List<(string Value, string Label)> AllDatabaseOptions =
  [
    ("local", "Local DB"),
    ("azure", "Azure DB")
  ];

  private IEnumerable<(string Value, string Label)> DatabaseOptions =>
    _isDevelopment ? AllDatabaseOptions : AllDatabaseOptions.Where(o => o.Value != "local");

  protected override async Task OnInitializedAsync()
  {
    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
    var user = authState.User;
    IsAdmin = user.IsInRole("Admin");

    try
    {
      if(BudgetEnvironment.IsAzureDatabase)
        _targetDatabase = "azure";
    }
    catch(Exception ex)
    {
      Logger.LogError(ex, "Error loading system info for restore options");
    }

    await LoadBackupSetsAsync();
  }

  private async Task LoadBackupSetsAsync()
  {
    try
    {
      Logger.LogInformation("Loading backup sets for restore...");
      _backupSets = [.. (await MaintApiClient.GetBackupSetsAsync())];
      Logger.LogInformation("Loaded {Count} backup sets", _backupSets.Count);
    }
    catch(HttpRequestException ex) when(ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
      Logger.LogError(ex, "Unauthorized error loading backup sets");
      Snackbar.Add("Authentication required. Please sign out and sign back in.", Severity.Warning);
      _backupSets = [];
    }
    catch(Exception ex)
    {
      Logger.LogError(ex, "Error loading backup sets");
      Snackbar.Add($"Error loading backup sets: {ex.Message}", Severity.Error);
      _backupSets = [];
    }
  }

  private async Task SelectBackupSet(BackupSetDto backupSet)
  {
    _selectedBackupSet = backupSet;
    _backupTables = null;

    try
    {
      Logger.LogInformation("Loading backup set details for: {PartitionKey}", backupSet.PartitionKey);
      _backupTables = [.. (await MaintApiClient.GetBackupSetDetailsAsync(backupSet.PartitionKey))];
      Logger.LogInformation("Loaded {Count} tables", _backupTables.Count);
    }
    catch(Exception ex)
    {
      Logger.LogError(ex, "Error loading backup set details");
      Snackbar.Add($"Error loading backup set details: {ex.Message}", Severity.Error);
      _backupTables = [];
    }
  }

  private async Task RestoreBackupSetAsync(BackupSetDto backupSet)
  {
    if(!IsAdmin)
    {
      Snackbar.Add("Admin privileges required for this operation", Severity.Error);
      return;
    }

    var targetLabel = DatabaseOptions.FirstOrDefault(o => o.Value == _targetDatabase).Label ?? _targetDatabase;

    var parameters = new DialogParameters {
      ["ContentText"] = $"Are you sure you want to restore the backup from {backupSet.BackupDate:yyyy-MM-dd HH:mm:ss} to {targetLabel}? This will overwrite ALL current data in the selected database and cannot be undone.",
      ["ButtonText"] = "Restore",
      ["Color"] = Color.Warning
    };

    var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
    var dialog = await DialogService.ShowAsync<ConfirmDialog>("Confirm Restore", parameters, options);
    var result = await dialog.Result;

    if(result is { Canceled: true })
      return;

    // Reset log and start restore
    _logMessages.Clear();
    RestoreBusy = true;
    StateHasChanged();

    try
    {
      Logger.LogInformation("Starting restore for backup set: {PartitionKey} to {TargetDatabase}", backupSet.PartitionKey, _targetDatabase);
      var response = await MaintApiClient.ImportAllAsync(backupSet.PartitionKey, _targetDatabase);
      _currentRestoreId = response.RestoreId;
      AddLogMessage(response.Message);

      Snackbar.Add("Restore started. Progress shown below.", Severity.Info);
      StartStatusPolling();
    }
    catch(Exception ex)
    {
      Logger.LogError(ex, "Error starting restore");
      AddLogMessage($"ERROR: {ex.Message}");
      Snackbar.Add($"Error starting restore: {ex.Message}", Severity.Error);
      RestoreBusy = false;
      StateHasChanged();
    }
  }

  private void AddLogMessage(string message)
  {
    _logMessages.Add(message);
  }

  private void StartStatusPolling()
  {
    _pollTimer = new System.Timers.Timer(2000); // Poll every 2 seconds
    _pollTimer.Elapsed += async (_, _) =>
    {
      try
      {
        await PollRestoreStatusAsync();
      }
      catch(Exception ex)
      {
        Logger.LogError(ex, "Error polling restore status");
        StopStatusPolling();
      }
    };
    _pollTimer.AutoReset = true;
    _pollTimer.Start();
  }

  private async Task PollRestoreStatusAsync()
  {
    if(string.IsNullOrEmpty(_currentRestoreId))
      return;

    try
    {
      var status = await MaintApiClient.GetRestoreStatusAsync(_currentRestoreId);
      if(status == null)
      {
        AddLogMessage("Restore status not found.");
        StopStatusPolling();
        return;
      }

      // Append any new log messages that arrived since last poll
      var currentCount = _logMessages.Count;
      var allMessages = status.LogMessages;
      for(int i = currentCount; i < allMessages.Count; i++)
        _logMessages.Add(allMessages[i]);

      if(status.IsComplete)
      {
        StopStatusPolling();
        RestoreBusy = false;

        if(!string.IsNullOrEmpty(status.ErrorMessage))
          Snackbar.Add($"Restore failed: {status.ErrorMessage}", Severity.Error);
        else
          Snackbar.Add($"Restore completed: {status.CompletedTables} table(s) restored.", Severity.Success);
      }

      await InvokeAsync(StateHasChanged);
    }
    catch(Exception ex)
    {
      Logger.LogError(ex, "Error checking restore status");
      StopStatusPolling();
      RestoreBusy = false;
      await InvokeAsync(StateHasChanged);
    }
  }

  private void StopStatusPolling()
  {
    if(_pollTimer != null)
    {
      _pollTimer.Stop();
      _pollTimer.Dispose();
      _pollTimer = null;
    }
  }

  private static string FormatBytes(long bytes)
  {
    string[] sizes = ["B", "KB", "MB", "GB"];
    double len = bytes;
    int order = 0;
    while(len >= 1024 && order < sizes.Length - 1)
    {
      order++;
      len /= 1024;
    }
    return $"{len:0.##} {sizes[order]}";
  }

  void IDisposable.Dispose()
  {
    StopStatusPolling();
    GC.SuppressFinalize(this);
  }
}
