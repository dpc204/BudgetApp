namespace Budget.Client.Components.Maintenance.BacpacHistory;

using Budget.Client.Components.Maintenance.BackupRestore;

/// <summary>
/// Component for viewing, triggering, downloading, and deleting BACPAC backups
/// </summary>
public partial class BacpacHistoryIndex : IDisposable
{
  [Inject] private IUtilitiesApiClient ApiClient { get; set; } = default!;
  [Inject] private ISnackbar Snackbar { get; set; } = default!;
  [Inject] private IDialogService DialogService { get; set; } = default!;
  [Inject] private IJSRuntime JS { get; set; } = default!;
  [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
  [Inject] private ILogger<BacpacHistoryIndex> Logger { get; set; } = default!;

  private List<BacpacBackupDto>? _backups;

  protected bool IsAdmin { get; private set; }
  protected bool Busy { get; private set; }
  protected string ButtonText { get; private set; } = "Run Backup Now";
  protected string? Status { get; private set; }
  protected Severity StatusSeverity { get; private set; } = Severity.Info;

  protected override async Task OnInitializedAsync()
  {
    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
    IsAdmin = authState.User.IsInRole("Admin");
    await LoadHistoryAsync();
  }

  private async Task LoadHistoryAsync()
  {
    try
    {
      Logger.LogInformation("Loading BACPAC history...");
      _backups = [.. (await ApiClient.GetBacpacHistoryAsync())];
      Logger.LogInformation("Loaded {Count} BACPAC backup records", _backups.Count);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
      Logger.LogError(ex, "Unauthorized loading BACPAC history");
      Snackbar.Add("Authentication required. Please sign out and sign back in.", Severity.Warning);
      _backups = [];
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error loading BACPAC history");
      Snackbar.Add($"Error loading BACPAC history: {ex.Message}", Severity.Error);
      _backups = [];
    }
  }

  protected async Task TriggerBackupAsync()
  {
    if (!IsAdmin)
    {
      Snackbar.Add("Admin privileges required for this operation", Severity.Error);
      return;
    }

    Busy = true;
    Status = null;
    ButtonText = "Running backup...";

    try
    {
      Logger.LogInformation("Triggering BACPAC backup...");
      var result = await ApiClient.TriggerBacpacBackupAsync();
      Status = "Backup completed successfully.";
      StatusSeverity = Severity.Success;
      Snackbar.Add("BACPAC backup completed successfully.", Severity.Success);
      Logger.LogInformation("BACPAC backup result: {Result}", result);

      await LoadHistoryAsync();
    }
    catch (Exception ex)
    {
      Status = $"Backup failed: {ex.Message}";
      StatusSeverity = Severity.Error;
      Snackbar.Add($"Backup failed: {ex.Message}", Severity.Error);
      Logger.LogError(ex, "Error triggering BACPAC backup");
    }
    finally
    {
      Busy = false;
      ButtonText = "Run Backup Now";
    }
  }

  private async Task DownloadBackupAsync(BacpacBackupDto backup)
  {
    try
    {
      Logger.LogInformation("Downloading BACPAC backup: {RowKey}", backup.RowKey);
      var fileDownload = await ApiClient.DownloadBacpacBackupAsync(backup.RowKey);

      var base64 = Convert.ToBase64String(fileDownload.Content);
      var dataUrl = $"data:{fileDownload.ContentType};base64,{base64}";
      await JS.InvokeVoidAsync("downloadFileFromStream", fileDownload.FileName, dataUrl);

      Snackbar.Add($"Downloaded {fileDownload.FileName}", Severity.Success);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error downloading BACPAC backup {RowKey}", backup.RowKey);
      Snackbar.Add($"Download failed: {ex.Message}", Severity.Error);
    }
  }

  private async Task DeleteBackupAsync(BacpacBackupDto backup)
  {
    var parameters = new DialogParameters
    {
      ["ContentText"] = $"Are you sure you want to delete the backup from {backup.CreatedAt:yyyy-MM-dd HH:mm:ss}? This cannot be undone.",
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
      Logger.LogInformation("Deleting BACPAC backup: {RowKey}", backup.RowKey);
      var success = await ApiClient.DeleteBacpacBackupAsync(backup.RowKey);

      if (success)
      {
        Snackbar.Add("Backup deleted successfully.", Severity.Success);
        await LoadHistoryAsync();
      }
      else
      {
        Snackbar.Add("Backup not found or already deleted.", Severity.Warning);
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error deleting BACPAC backup {RowKey}", backup.RowKey);
      Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
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
      len /= 1024;
    }
    return $"{len:0.##} {sizes[order]}";
  }

  void IDisposable.Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
