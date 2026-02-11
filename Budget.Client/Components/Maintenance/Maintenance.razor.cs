namespace Budget.Client.Components.Maintenance;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

public partial class Maintenance : IDisposable
{
  [Inject] private IUtilitiesApiClient MaintApiClient { get; set; } = default!;
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

      ButtonText = "Downloading...";
      
      // Download file via HttpClient
      var fileDownload = await MaintApiClient.DownloadDatabaseBackupAsync(fileName);
      
      // Convert to base64 for JavaScript download
      var base64 = Convert.ToBase64String(fileDownload.Content);
      var dataUrl = $"data:{fileDownload.ContentType};base64,{base64}";
      
      await JS.InvokeVoidAsync("downloadFileFromStream", fileDownload.FileName, dataUrl);
      
      Snackbar.Add("Backup export complete. File downloaded.", Severity.Success);
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
    GC.SuppressFinalize(this);
  }
}