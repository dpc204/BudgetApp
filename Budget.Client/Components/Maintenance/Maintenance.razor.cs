namespace Budget.Client.Components.Maintenance;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Timers;

public partial class Maintenance : IDisposable
{
  [Inject] private IBudgetMaintApiClient MaintApiClient { get; set; } = default!;
  [Inject] private ISnackbar Snackbar { get; set; } = default!;
  [Inject] private NavigationManager Nav { get; set; } = default!;
  [Inject] private IJSRuntime JS { get; set; } = default!;
  [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

  protected bool Busy { get; private set; }
  protected string ButtonText { get; private set; } = "Backup Azure SQL Database";
  protected string? Status { get; private set; }
  
  protected bool BackupAllBusy { get; private set; }
  protected string BackupAllButtonText { get; private set; } = "Backup All Tables";
  protected string? BackupAllStatus { get; private set; }
  protected string? CurrentBackupId { get; private set; }
  protected bool IsAdmin { get; private set; }
  
  private System.Timers.Timer? _pollTimer;

  protected override async Task OnInitializedAsync()
  {
    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
    var user = authState.User;
    IsAdmin = user.IsInRole("Admin");
  }

  protected async Task TriggerBackupAsync()
  {
    Busy = true;
    Status = null;
    ButtonText = "Preparing download...";
    try
    {
      // Ask server for the filename it will use
      var plan = await MaintApiClient.GetBackupPlanAsync();
      var fileName = plan.FileName;

      // Start download with agreed filename
      var url = $"/api/maintenance/backup-download?name={Uri.EscapeDataString(fileName)}";
      await JS.InvokeVoidAsync("open", url, "_blank");
      Snackbar.Add("Backup export started. Your browser should download the .bacpac.", Severity.Success);
      Status = $"Backup: {fileName} downloaded";
    }
    catch (Exception ex)
    {
      Status = ex.Message;
      Snackbar.Add(Status, Severity.Error);
    }
    finally
    {
      Busy = false;
      ButtonText = "Backup Azure SQL Database";
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

  public void Dispose()
  {
    StopStatusPolling();
  }
}