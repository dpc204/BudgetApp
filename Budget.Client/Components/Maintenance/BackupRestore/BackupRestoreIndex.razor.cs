namespace Budget.Client.Components.Maintenance.BackupRestore;

public partial class BackupRestoreIndex : IDisposable
{
  [Inject] private IBudgetMaintApiClient MaintApiClient { get; set; } = default!;
  [Inject] private ISnackbar Snackbar { get; set; } = default!;
  [Inject] private IDialogService DialogService { get; set; } = default!;
  [Inject] private IJSRuntime JS { get; set; } = default!;
  [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
  [Inject] private ILogger<BackupRestoreIndex> Logger { get; set; } = default!;

  private List<BackupSetDto>? _backupSets;
  private List<BackupTableDto>? _backupTables;
  private BackupSetDto? _selectedBackupSet;

  protected bool IsAdmin { get; private set; }
  protected bool BackupAllBusy { get; private set; }
  protected string BackupAllButtonText { get; private set; } = "Backup All Tables";
  protected string? BackupAllStatus { get; private set; }
  protected string? CurrentBackupId { get; private set; }

  private System.Timers.Timer? _pollTimer;

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
      Logger.LogInformation("Loading backup sets...");
      _backupSets = (await MaintApiClient.GetBackupSetsAsync()).ToList();
      Logger.LogInformation("Loaded {Count} backup sets", _backupSets.Count);
    }
    catch (Exception ex)
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
      _backupTables = (await MaintApiClient.GetBackupSetDetailsAsync(backupSet.PartitionKey)).ToList();
      Logger.LogInformation("Loaded {Count} tables", _backupTables.Count);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error loading backup set details");
      Snackbar.Add($"Error loading backup set details: {ex.Message}", Severity.Error);
      _backupTables = [];
    }
  }

  private async Task DeleteBackupSetAsync(BackupSetDto backupSet)
  {
    var parameters = new DialogParameters
    {
      ["ContentText"] = $"Are you sure you want to delete the backup set from {backupSet.BackupDate:yyyy-MM-dd HH:mm:ss}? This will delete {backupSet.TableCount} table backups and cannot be undone.",
      ["ButtonText"] = "Delete",
      ["Color"] = Color.Error
    };

    var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
    var dialog = await DialogService.ShowAsync<ConfirmDialog>("Confirm Delete", parameters, options);
    var result = await dialog.Result;

    if (result is { Canceled: true })
      return;

    try
    {
      Logger.LogInformation("Deleting backup set: {PartitionKey}", backupSet.PartitionKey);
      var success = await MaintApiClient.DeleteBackupSetAsync(backupSet.PartitionKey);
      
      if (success)
      {
        Snackbar.Add("Backup set deleted successfully", Severity.Success);
        
        // Clear selection if we deleted the selected set
        if (_selectedBackupSet?.PartitionKey == backupSet.PartitionKey)
        {
          _selectedBackupSet = null;
          _backupTables = null;
        }
        
        // Reload the backup sets list
        await LoadBackupSetsAsync();
      }
      else
      {
        Snackbar.Add("Failed to delete backup set", Severity.Error);
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error deleting backup set");
      Snackbar.Add($"Error deleting backup set: {ex.Message}", Severity.Error);
    }
  }

  private async Task DownloadCsvAsync(BackupTableDto table)
  {
    try
    {
      Logger.LogInformation("Downloading CSV for table: {TableName}", table.TableName);
      
      var fileDownload = await MaintApiClient.DownloadBackupCsvAsync(table.BlobName);
      
      // Convert to base64 for JavaScript download
      var base64 = Convert.ToBase64String(fileDownload.Content);
      var dataUrl = $"data:{fileDownload.ContentType};base64,{base64}";
      
      await JS.InvokeVoidAsync("downloadFileFromStream", fileDownload.FileName, dataUrl);
      Snackbar.Add($"Downloaded {table.TableName}.csv", Severity.Success);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error downloading CSV");
      Snackbar.Add($"Error downloading CSV: {ex.Message}", Severity.Error);
    }
  }

  protected async Task TriggerBackupAllTablesAsync()
  {
    if (!IsAdmin)
    {
      Snackbar.Add("Admin privileges required for this operation", Severity.Error);
      return;
    }

    BackupAllBusy = true;
    BackupAllStatus = "Starting backup...";
    BackupAllButtonText = "Backing up...";

    try
    {
      var response = await MaintApiClient.ExportAllTablesAsync();
      CurrentBackupId = response.BackupId;
      BackupAllStatus = response.Message;
      Snackbar.Add("Backup started successfully. Progress will be displayed below.", Severity.Success);

      // Start polling for status
      StartStatusPolling();
    }
    catch (Exception ex)
    {
      BackupAllStatus = $"Error: {ex.Message}";
      Snackbar.Add($"Failed to start backup: {ex.Message}", Severity.Error);
      BackupAllBusy = false;
      BackupAllButtonText = "Backup All Tables";
    }
  }

  private void StartStatusPolling()
  {
    _pollTimer = new System.Timers.Timer(2000); // Poll every 2 seconds
    _pollTimer.Elapsed += async (sender, e) =>
    {
      try
      {
        await PollBackupStatusAsync();
      }
      catch (Exception ex)
      {
        BackupAllStatus = $"Error polling status: {ex.Message}";
        StopStatusPolling();
      }
    };
    _pollTimer.AutoReset = true;
    _pollTimer.Start();
  }

  private async Task PollBackupStatusAsync()
  {
    if (string.IsNullOrEmpty(CurrentBackupId))
      return;

    try
    {
      var status = await MaintApiClient.GetBackupStatusAsync(CurrentBackupId);
      if (status == null)
      {
        BackupAllStatus = "Backup status not found";
        StopStatusPolling();
        return;
      }

      if (status.IsComplete)
      {
        BackupAllStatus = $"Backup completed! Tables: {status.CompletedTables}/{status.TotalTables}, Failed: {status.FailedTables}";
        BackupAllBusy = false;
        BackupAllButtonText = "Backup All Tables";
        StopStatusPolling();

        if (status.FailedTables > 0)
        {
          Snackbar.Add($"Backup completed with {status.FailedTables} failures", Severity.Warning);
        }
        else
        {
          Snackbar.Add("Backup completed successfully!", Severity.Success);
        }

        // Reload backup sets to show the new backup
        await LoadBackupSetsAsync();
      }
      else
      {
        var progress = status.TotalTables > 0
          ? $"{status.CompletedTables}/{status.TotalTables}"
          : "Initializing...";
        BackupAllStatus = $"Progress: {progress} - Current: {status.CurrentTable ?? "Preparing..."}";

        if (!string.IsNullOrEmpty(status.ErrorMessage))
        {
          BackupAllStatus += $" - {status.ErrorMessage}";
        }
      }

      await InvokeAsync(StateHasChanged);
    }
    catch (Exception ex)
    {
      BackupAllStatus = $"Error checking status: {ex.Message}";
      StopStatusPolling();
    }
  }

  private void StopStatusPolling()
  {
    if (_pollTimer != null)
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
    while (len >= 1024 && order < sizes.Length - 1)
    {
      order++;
      len = len / 1024;
    }
    return $"{len:0.##} {sizes[order]}";
  }

  public void Dispose()
  {
    StopStatusPolling();
  }
}
