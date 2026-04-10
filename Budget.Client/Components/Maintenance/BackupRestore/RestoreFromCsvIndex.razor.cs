namespace Budget.Client.Components.Maintenance.BackupRestore;

public partial class RestoreFromCsvIndex : IDisposable
{
  [Inject] private IUtilitiesApiClient MaintApiClient { get; set; } = null!;
  [Inject] private ISnackbar Snackbar { get; set; } = null!;
  [Inject] private IDialogService DialogService { get; set; } = null!;
  [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
  [Inject] private ILogger<RestoreFromCsvIndex> Logger { get; set; } = null!;

  private List<BackupSetDto>? _backupSets;
  private List<BackupTableDto>? _backupTables;
  private BackupSetDto? _selectedBackupSet;
  private string _targetDatabase = "local";

  private bool IsAdmin { get; set; }
  private bool RestoreBusy { get; set; }

  private static readonly List<(string Value, string Label)> DatabaseOptions =
  [
    ("local", "Local DB"),
    ("azure", "Azure DB")
  ];

  protected override async Task OnInitializedAsync()
  {
    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
    var user = authState.User;
    IsAdmin = user.IsInRole("Admin");

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

    RestoreBusy = true;
    StateHasChanged();

    try
    {
      Logger.LogInformation("Restoring backup set: {PartitionKey} to {TargetDatabase}", backupSet.PartitionKey, _targetDatabase);
      var response = await MaintApiClient.ImportAllAsync(backupSet.PartitionKey, _targetDatabase);

      if(response.Success)
      {
        Snackbar.Add($"Successfully restored {response.TablesRestored} tables to {targetLabel}", Severity.Success);
      }
      else
      {
        var errorDetails = response.Errors.Count > 0 ? string.Join("; ", response.Errors) : response.Message;
        Snackbar.Add($"Restore failed: {errorDetails}", Severity.Error);
      }
    }
    catch(Exception ex)
    {
      Logger.LogError(ex, "Error during restore");
      Snackbar.Add($"Error during restore: {ex.Message}", Severity.Error);
    }
    finally
    {
      RestoreBusy = false;
      StateHasChanged();
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
    GC.SuppressFinalize(this);
  }
}
