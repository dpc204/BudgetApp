namespace Budget.Client.Components.Import;

public partial class TransactionsCsvImport : ComponentBase
{
  [Inject] protected IUserAndOptions UserAndOptions { get; set; } = default!;
  [Inject] protected ITransactionsApiClient Api { get; set; } = default!;
  [Inject] protected IAccountsApiClient AccountsApi { get; set; } = default!;
  [Inject] protected ISnackbar Snackbar { get; set; } = default!;
  [Inject] protected IBudgetMonthlyApiClient BudgetMonthlyApi { get; set; } = default!;
  [Inject] protected IDialogService DialogService { get; set; } = default!;
  protected bool Busy { get; set; }
  protected string Status { get; set; } = string.Empty;
  protected int ParsedRowsCount => Preview.Count;
  protected int Value { get; set; }

  protected IBrowserFile? SelectedFile { get; set; }
  protected List<string> Errors { get; } = [];
  protected List<TransactionImportDto> Preview { get; } = [];

  protected List<BankAccountDto> Accounts { get; } = [];
  protected int SelectedAccountId { get; set; }
  protected string UserIdText { get; set; } = "1"; // default for now

  protected override async Task OnInitializedAsync()
  {
    var accounts = await AccountsApi.GetAccountsAsync();
    Accounts.Clear();
    Accounts.AddRange(accounts);
    SelectedAccountId = UserAndOptions.Options.PreviousImportAccount;

    // Load any existing staged imports when page loads
    await LoadPreviewAsync();
  }

  protected async Task OnInputFileChange(InputFileChangeEventArgs e)
  {
    Errors.Clear();
    Preview.Clear();
    SelectedFile = e.File;

    if(SelectedFile is null)
    {
      return;
    }

    if(!SelectedFile.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
    {
      Errors.Add("Only .csv files are supported.");
      return;
    }

    try
    {
      await using var stream = SelectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
      using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

      var lines = new List<string>();
      while(true)
      {
        var line = await reader.ReadLineAsync();
        if(line is null) break; // avoid EndOfStream sync checks
        lines.Add(line);
      }

      if(lines.Count == 0)
      {
        Errors.Add("The file is empty.");
        return;
      }

      // parse header
      var headers = ParseCsvLine(lines[0]);
      var map = BuildHeaderMap(headers);
      if(map.Count == 0)
      {
        Errors.Add("No recognized headers. Expected at least: date,vendor,description,amount,envelope or envelopeid");
        return;
      }

      var transactionsToImport = new List<TransactionImportDto>();

      for(int i = 1; i < lines.Count; i++)
      {
        var row = ParseCsvLine(lines[i]);
        if(row.Count == 0 || row.All(string.IsNullOrWhiteSpace)) continue;

        try
        {
          Debug.WriteLine($"Line {i}");
          var dto = MapRowToTransactionImportDto(row, map);
          transactionsToImport.Add(dto);
        }
        catch(Exception ex)
        {
          Errors.Add($"Line {i + 1}: {ex.Message} FullLine: {lines[i]}");
        }
      }

      // Import to database in bulk
      if(transactionsToImport.Count > 0)
      {
        var count = await Api.ImportTransactionsToStagingAsync(transactionsToImport);
        Status = $"Imported {count} rows to staging.";

        // Reload preview from database
        await LoadPreviewAsync();
      }
      else
      {
        Status = "No valid rows to import.";
      }
    }
    catch(Exception ex)
    {
      Errors.Add(ex.Message);
    }
  }

  private async Task LoadPreviewAsync()
  {
    try
    {
      Busy = true;
      await InvokeAsync(StateHasChanged);
      Preview.Clear();
      var imports = await Api.GetTransactionImportsAsync();
      Preview.AddRange(imports);
    }
    finally
    {
      Busy = false;
    }
  }

  protected async Task ImportAsync()
  {
    if(Preview.Count == 0 || SelectedAccountId == 0)
    {
      return;
    }



    // Filter out duplicates
    var nonDuplicates = Preview.Where(p => !p.Duplicate || (p.Duplicate && p.KeepDuplicate)).ToList();

    if(nonDuplicates.Count == 0)
    {
      Snackbar.Add("No non-duplicate transactions to import", Severity.Warning);
      return;
    }

    var confirmed = await DialogService.ShowMessageBoxAsync(
      "Confirm Import",
      "This will import all non-duplicate transactions into the Unassigned Envelope.  Continue?",
      yesText: "Continue",
      cancelText: "Cancel") ?? false;

    if(!confirmed)
    {
      return;
    }

    UserAndOptions.Options.PreviousImportAccount = SelectedAccountId;

    Busy = true;
    await InvokeAsync(StateHasChanged);

    try
    {
      int userId = int.TryParse(UserIdText, out var uid) ? uid : 1;
      Status = "Loading transactions to Unassigned";
      // Call the new API endpoint to load all imports to Unassigned
      var importedCount = await Api.LoadTransactionImportsToUnassignedAsync(SelectedAccountId, userId);

      if(importedCount > 0)
      {
        Snackbar.Add($"Imported {importedCount} transactions", Severity.Success);
        Status = $"Loaded {importedCount} transactions to Unassigned.";
        Preview.Clear();
        SelectedFile = null;
      }
      else
      {
        Snackbar.Add("No transactions were imported", Severity.Warning);
      }
    }
    catch(Exception ex)
    {
      Errors.Add(ex.Message);
      Snackbar.Add($"Error importing transactions: {ex.Message}", Severity.Error);
    }

    finally
    {
      Busy = false;
      Value = 0;
      await InvokeAsync(StateHasChanged);
    }
  }


  protected async Task UpdateTransaction(TransactionImportDto import)
  {
    // Save the change immediately
    var success = await Api.UpdateTransactionImportAsync(import.Id, import.Duplicate, import.KeepDuplicate);
    if(success)
    {
      await InvokeAsync(StateHasChanged);
    }
    else
    {
      Errors.Add($"Failed to update duplicate flag for transaction {import.Id}");
    }
  }

  protected async Task DeleteStagedTransactionsAsync()
  {
    var confirmed = await DialogService.ShowMessageBoxAsync(
      "Confirm Delete",
      "Are you sure you want to delete all staged transactions?",
      yesText: "Delete",
      cancelText: "Cancel");

    if(confirmed != true)
    {
      return;
    }

    Busy = true;
    try
    {
      var count = await Api.ClearTransactionImportsAsync();
      Preview.Clear();
      Status = $"Deleted {count} staged transactions.";
      Snackbar.Add($"Deleted {count} staged transactions", Severity.Success);
    }
    catch(Exception ex)
    {
      Errors.Add($"Failed to delete staged transactions: {ex.Message}");
      Snackbar.Add("Failed to delete staged transactions", Severity.Error);
    }
    finally
    {
      Busy = false;
      await InvokeAsync(StateHasChanged);
    }
  }

  private static Dictionary<string, int> BuildHeaderMap(List<string> headers)
  {
    var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    for(int i = 0; i < headers.Count; i++)
    {
      var h = headers[i].Trim();
      if(string.IsNullOrEmpty(h)) continue;
      map[h] = i;

      // alias common names
      if(h.Equals("date", StringComparison.OrdinalIgnoreCase)) map["date"] = i;
      if(h.Equals("vendor", StringComparison.OrdinalIgnoreCase)) map["vendor"] = i;
      if(h.Equals("description", StringComparison.OrdinalIgnoreCase)) map["description"] = i;
      if(h.Equals("notes", StringComparison.OrdinalIgnoreCase)) map["notes"] = i;
      if(h.Equals("amount", StringComparison.OrdinalIgnoreCase) ||
          h.Equals("total", StringComparison.OrdinalIgnoreCase) ||
          h.Equals("debit", StringComparison.OrdinalIgnoreCase)) map["amount"] = i;
      if(h.Equals("envelope", StringComparison.OrdinalIgnoreCase)) map["envelope"] = i;
      if(h.Equals("category", StringComparison.OrdinalIgnoreCase)) map["category"] = i;
      if(h.Equals("envelopeid", StringComparison.OrdinalIgnoreCase)) map["envelopeid"] = i;
      if(h.Equals("categoryid", StringComparison.OrdinalIgnoreCase)) map["categoryid"] = i;
      if(h.Equals("userid", StringComparison.OrdinalIgnoreCase)) map["userid"] = i;
    }

    return map;
  }

  private static TransactionDto MapRowToTransactionDto(List<string> row, Dictionary<string, int> map)
  {
    DateTime date = DateTime.Today;
    string vendor = string.Empty;
    string desc = string.Empty;
    decimal amount = 0m;
    string envelopeName = string.Empty;
    int envelopeId = 0;
    int userId = 0;

    if(map.TryGetValue("date", out var idxDate) && idxDate < row.Count)
    {
      var txt = row[idxDate];
      if(!DateTime.TryParse(txt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
      {
        // try current culture too
        date = DateTime.Parse(txt, CultureInfo.CurrentCulture);
      }
    }

    if(map.TryGetValue("vendor", out var idxVendor) && idxVendor < row.Count)
      vendor = row[idxVendor];

    if(map.TryGetValue("description", out var idxDesc) && idxDesc < row.Count)
      desc = row[idxDesc];

    if(map.TryGetValue("amount", out var idxAmt) && idxAmt < row.Count)
    {
      var raw = row[idxAmt].Replace("$", string.Empty).Replace(",", string.Empty);
      amount = decimal.Parse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
        CultureInfo.InvariantCulture);
    }

    if(map.TryGetValue("envelope", out var idxEnv) && idxEnv < row.Count)
      envelopeName = row[idxEnv];

    if(map.TryGetValue("envelopeid", out var idxEnvId) && idxEnvId < row.Count)
      _ = int.TryParse(row[idxEnvId], out envelopeId);

    if(map.TryGetValue("userid", out var idxUserId) && idxUserId < row.Count)
      _ = int.TryParse(row[idxUserId], out userId);

    return new TransactionDto {
      Vendor = vendor,
      Description = desc,
      Amount = amount,
      Date = date,
      EnvelopeId = envelopeId,
      EnvelopeName = envelopeName,
      UserId = userId
    };
  }

  private static TransactionImportDto MapRowToTransactionImportDto(List<string> row, Dictionary<string, int> map)
  {
    DateTime date = DateTime.Today;
    string vendor = string.Empty;
    string desc = string.Empty;
    decimal amount = 0m;
    string envelopeName = string.Empty;
    int envelopeId = 0;
    int userId = 0;

    if(map.TryGetValue("posting date", out var idxDate) && idxDate < row.Count)
    {
      var txt = row[idxDate];
      if(!DateTime.TryParse(txt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
      {
        // try current culture too
        date = DateTime.Parse(txt, CultureInfo.CurrentCulture);
      }
    }

    if(map.TryGetValue("vendor", out var idxVendor) && idxVendor < row.Count)
      vendor = row[idxVendor];

    if(map.TryGetValue("description", out var idxDesc) && idxDesc < row.Count)
      desc = row[idxDesc];

    if(map.TryGetValue("amount", out var idxAmt) && idxAmt < row.Count)
    {
      var raw = row[idxAmt].Replace("$", string.Empty).Replace(",", string.Empty);
      amount = decimal.Parse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
        CultureInfo.InvariantCulture);
    }

    if(map.TryGetValue("envelope", out var idxEnv) && idxEnv < row.Count)
      envelopeName = row[idxEnv];

    if(map.TryGetValue("envelopeid", out var idxEnvId) && idxEnvId < row.Count)
      _ = int.TryParse(row[idxEnvId], out envelopeId);

    if(map.TryGetValue("userid", out var idxUserId) && idxUserId < row.Count)
      _ = int.TryParse(row[idxUserId], out userId);

    return new TransactionImportDto {
      Vendor = vendor,
      Description = desc,
      Amount = amount,
      Date = date,
      EnvelopeId = envelopeId,
      EnvelopeName = envelopeName,
      UserId = userId
    };
  }

  private static List<string> ParseCsvLine(string line)
  {
    var result = new List<string>();
    if(string.IsNullOrEmpty(line)) return result;

    var sb = new StringBuilder();
    bool inQuotes = false;

    for(int i = 0; i < line.Length; i++)
    {
      var c = line[i];
      if(c == '"')
      {
        if(inQuotes && i + 1 < line.Length && line[i + 1] == '"')
        {
          // escaped quote
          sb.Append('"');
          i++; // skip
        }
        else
        {
          inQuotes = !inQuotes;
        }
      }
      else if(c == ',' && !inQuotes)
      {
        result.Add(sb.ToString());
        sb.Clear();
      }
      else
      {
        sb.Append(c);
      }
    }

    result.Add(sb.ToString());

    // trim whitespace
    for(int j = 0; j < result.Count; j++)
    {
      result[j] = result[j].Trim();
    }

    return result;
  }
}