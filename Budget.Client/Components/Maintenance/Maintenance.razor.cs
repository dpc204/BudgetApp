namespace Budget.Client.Components.Maintenance;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

public partial class Maintenance : IDisposable
{
  [Inject] private IBudgetMaintApiClient MaintApiClient { get; set; } = default!;
  [Inject] private ISnackbar Snackbar { get; set; } = default!;
  [Inject] private NavigationManager Nav { get; set; } = default!;
  [Inject] private IJSRuntime JS { get; set; } = default!;

  protected bool Busy { get; private set; }
  protected string ButtonText { get; private set; } = "Backup Azure SQL Database";
  protected string? Status { get; private set; }

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

  public void Dispose()
  {
    // No resources to dispose
  }
}