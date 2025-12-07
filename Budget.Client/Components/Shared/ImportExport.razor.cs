namespace Budget.Client.Components.Shared;

public partial class ImportExport<T> : ComponentBase where T : class
{
  [Parameter, EditorRequired]
  public string EntityName { get; set; } = string.Empty;

  [Parameter, EditorRequired]
  public Func<string, CancellationToken, Task<(int ImportedCount, List<string> Errors)>> ImportFunc { get; set; } = null!;

  [Parameter, EditorRequired]
  public Func<CancellationToken, Task<string>> ExportFunc { get; set; } = null!;

  [Parameter]
  public EventCallback OnImportCompleted { get; set; }

  [Parameter]
  public string Class { get; set; } = string.Empty;

  [Inject]
  private ISnackbar Snackbar { get; set; } = null!;

  [Inject]
  private IJSRuntime JSRuntime { get; set; } = null!;

  protected bool Busy { get; set; }
  protected string Status { get; set; } = string.Empty;
  protected Severity StatusSeverity { get; set; } = Severity.Info;
  protected IBrowserFile? SelectedFile { get; set; }
  protected List<string> Errors { get; } = [];

  // Key to force InputFile to reset after import/export
  private int _inputFileKey = 0;

  protected async Task OnInputFileChange(InputFileChangeEventArgs e)
  {
    Errors.Clear();
    Status = string.Empty;
    SelectedFile = e.File;

    if (SelectedFile is null)
    {
      return;
    }

    if (!SelectedFile.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
    {
      Errors.Add("Only .csv files are supported.");
      SelectedFile = null;
      return;
    }

    Status = $"Selected: {SelectedFile.Name}";
    StatusSeverity = Severity.Info;
  }

  protected async Task ImportAsync()
  {
    if (SelectedFile is null)
    {
      return;
    }

    Busy = true;
    Errors.Clear();
    Status = "Reading file...";
    StatusSeverity = Severity.Info;
    await InvokeAsync(StateHasChanged);

    try
    {
      await using var stream = SelectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
      using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
      var csvContent = await reader.ReadToEndAsync();

      Status = $"Importing {EntityName}...";
      await InvokeAsync(StateHasChanged);

      var (importedCount, errors) = await ImportFunc(csvContent, default);

      if (errors.Count > 0)
      {
        Errors.AddRange(errors);
        Status = $"Import completed with errors. {importedCount} {EntityName} imported.";
        StatusSeverity = Severity.Warning;
      }
      else
      {
        Status = $"Successfully imported {importedCount} {EntityName}.";
        StatusSeverity = Severity.Success;
        Snackbar.Add(Status, Severity.Success);
        
        // Notify parent to refresh data
        await OnImportCompleted.InvokeAsync();
      }

      SelectedFile = null;
      _inputFileKey++; // Reset the input file
    }
    catch (Exception ex)
    {
      Errors.Add($"Import failed: {ex.Message}");
      Status = "Import failed.";
      StatusSeverity = Severity.Error;
    }
    finally
    {
      Busy = false;
      await InvokeAsync(StateHasChanged);
    }
  }

  protected async Task ExportAsync()
  {
    Busy = true;
    Errors.Clear();
    Status = $"Exporting {EntityName}...";
    StatusSeverity = Severity.Info;
    await InvokeAsync(StateHasChanged);

    try
    {
      var csv = await ExportFunc(default);

      if (string.IsNullOrEmpty(csv))
      {
        Status = $"No {EntityName} to export.";
        StatusSeverity = Severity.Warning;
        Snackbar.Add(Status, Severity.Warning);
      }
      else
      {
        // Download the CSV file
        var fileName = $"{EntityName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetBytes(csv);
        var base64 = Convert.ToBase64String(bytes);
        
        await JSRuntime.InvokeVoidAsync("downloadFile", fileName, "text/csv", base64);
        
        Status = $"Successfully exported {EntityName}.";
        StatusSeverity = Severity.Success;
        Snackbar.Add(Status, Severity.Success);
      }
    }
    catch (Exception ex)
    {
      Errors.Add($"Export failed: {ex.Message}");
      Status = "Export failed.";
      StatusSeverity = Severity.Error;
      Snackbar.Add("Export failed: " + ex.Message, Severity.Error);
    }
    finally
    {
      Busy = false;
      await InvokeAsync(StateHasChanged);
    }
  }
}
