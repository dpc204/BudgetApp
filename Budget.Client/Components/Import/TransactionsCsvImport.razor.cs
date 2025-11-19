namespace Budget.Client.Components.Import;

public partial class TransactionsCsvImport : ComponentBase
{
  [Inject] protected IBudgetApiClient Api { get; set; } = default!;
  [Inject] protected ISnackbar Snackbar { get; set; } = default!;

  protected bool Busy { get; set; }
  protected string Status { get; set; } = string.Empty;
  protected int ParsedRowsCount => Preview.Count;
  protected int _value { get; set; }

  protected IBrowserFile? SelectedFile { get; set; }
  protected List<string> Errors { get; } = [];
  protected List<TransactionDto> Preview { get; } = [];

  protected List<BankAccountDto> Accounts { get; } = [];
  protected int SelectedAccountId { get; set; }
  protected string UserIdText { get; set; } = "1"; // default for now

  protected override async Task OnInitializedAsync()
  {
    var accounts = await Api.GetAccountsAsync();
    Accounts.Clear();
    Accounts.AddRange(accounts);
    SelectedAccountId = Accounts.FirstOrDefault()?.Id ?? 0;
  }

  protected async Task OnInputFileChange(InputFileChangeEventArgs e)
  {
    Errors.Clear();
    Preview.Clear();
    SelectedFile = e.File;

    if (SelectedFile is null)
    {
      return;
    }

    if (!SelectedFile.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
    {
      Errors.Add("Only .csv files are supported.");
      return;
    }

    try
    {
      await using var stream = SelectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
      using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

      var lines = new List<string>();
      while (true)
      {
        var line = await reader.ReadLineAsync();
        if (line is null) break; // avoid EndOfStream sync checks
        lines.Add(line);
      }

      if (lines.Count == 0)
      {
        Errors.Add("The file is empty.");
        return;
      }

      // parse header
      var headers = ParseCsvLine(lines[0]);
      var map = BuildHeaderMap(headers);
      if (map.Count == 0)
      {
        Errors.Add("No recognized headers. Expected at least: date,vendor,description,amount,envelope or envelopeid");
        return;
      }

      for (int i = 1; i < lines.Count; i++)
      {
        var row = ParseCsvLine(lines[i]);
        if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace)) continue;

        try
        {
          Debug.WriteLine($"Line {i}");
          var dto = MapRowToTransactionDto(row, map);
          Preview.Add(dto);
        }
        catch (Exception ex)
        {
          Errors.Add($"Line {i + 1}: {ex.Message} FullLine: {lines[i]}");
        }
      }

      Status = $"Parsed {Preview.Count} rows.";
    }
    catch (Exception ex)
    {
      Errors.Add(ex.Message);
    }
  }

  protected async Task ImportAsync()
  {
    if (Preview.Count == 0 || SelectedAccountId == 0)
    {
      return;
    }

    Busy = true;
    _value = 0;
    await InvokeAsync(StateHasChanged);

    try
    {
      var allEnvelopes = await Api.GetEnvelopesAsync();
      var envelopeByName = allEnvelopes.ToDictionary(e => e.Name, e => e.Id, StringComparer.OrdinalIgnoreCase);

      int totalCount = Preview.Count;
      int currentIndex = 0;

      foreach (var rec in Preview)
      {
        var trans = new OneTransactionDetail
        {
          AccountId = SelectedAccountId,
          Date = rec.Date,
          Vendor = rec.Vendor,
          UserId = int.TryParse(UserIdText, out var uid) ? uid : 1,
          UserName = string.Empty,
          Details = new List<TransactionDto>()
        };


        trans.Details.Add(new TransactionDto
        {
          LineId = 0,
          Vendor = rec.Vendor,
          Description = rec.Description,
          Amount = rec.Amount,
          Date = rec.Date,
          EnvelopeId = -1,
          EnvelopeName = rec.EnvelopeName,
          UserId = rec.UserId
        });


        await Api.AddTransactionAsync(trans);

        currentIndex++;
        _value = (int)((currentIndex / (double)totalCount) * 100);
        await InvokeAsync(StateHasChanged);
      }

      Snackbar.Add($"Imported {Preview.Count} items across  transactions", Severity.Success);
      Status = "Import complete.";
      Preview.Clear();
      SelectedFile = null;
    }
    catch (Exception ex)
    {
      Errors.Add(ex.Message);
    }

    finally
    {
      Busy = false;
      _value = 0;
      await InvokeAsync(StateHasChanged);
    }
  }

  private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> headers)
  {
    var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    for (int i = 0; i < headers.Count; i++)
    {
      var h = headers[i].Trim();
      if (string.IsNullOrEmpty(h)) continue;
      map[h] = i;

      // alias common names
      if (h.Equals("date", StringComparison.OrdinalIgnoreCase)) map["date"] = i;
      if (h.Equals("vendor", StringComparison.OrdinalIgnoreCase)) map["vendor"] = i;
      if (h.Equals("description", StringComparison.OrdinalIgnoreCase)) map["description"] = i;
      if (h.Equals("notes", StringComparison.OrdinalIgnoreCase)) map["notes"] = i;
      if (h.Equals("amount", StringComparison.OrdinalIgnoreCase) ||
          h.Equals("total", StringComparison.OrdinalIgnoreCase) ||
          h.Equals("debit", StringComparison.OrdinalIgnoreCase)) map["amount"] = i;
      if (h.Equals("envelope", StringComparison.OrdinalIgnoreCase)) map["envelope"] = i;
      if (h.Equals("category", StringComparison.OrdinalIgnoreCase)) map["category"] = i;
      if (h.Equals("envelopeid", StringComparison.OrdinalIgnoreCase)) map["envelopeid"] = i;
      if (h.Equals("categoryid", StringComparison.OrdinalIgnoreCase)) map["categoryid"] = i;
      if (h.Equals("userid", StringComparison.OrdinalIgnoreCase)) map["userid"] = i;
    }

    return map;
  }

  private static TransactionDto MapRowToTransactionDto(IReadOnlyList<string> row, Dictionary<string, int> map)
  {
    DateTime date = DateTime.Today;
    string vendor = string.Empty;
    string desc = string.Empty;
    decimal amount = 0m;
    string envelopeName = string.Empty;
    int envelopeId = 0;
    int userId = 0;

    if (map.TryGetValue("date", out var idxDate) && idxDate < row.Count)
    {
      var txt = row[idxDate];
      if (!DateTime.TryParse(txt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
      {
        // try current culture too
        date = DateTime.Parse(txt, CultureInfo.CurrentCulture);
      }
    }

    if (map.TryGetValue("vendor", out var idxVendor) && idxVendor < row.Count)
      vendor = row[idxVendor];

    if (map.TryGetValue("description", out var idxDesc) && idxDesc < row.Count)
      desc = row[idxDesc];

    if (map.TryGetValue("amount", out var idxAmt) && idxAmt < row.Count)
    {
      var raw = row[idxAmt].Replace("$", string.Empty).Replace(",", string.Empty);
      amount = decimal.Parse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
        CultureInfo.InvariantCulture);
    }

    if (map.TryGetValue("envelope", out var idxEnv) && idxEnv < row.Count)
      envelopeName = row[idxEnv];

    if (map.TryGetValue("envelopeid", out var idxEnvId) && idxEnvId < row.Count)
      int.TryParse(row[idxEnvId], out envelopeId);

    if (map.TryGetValue("userid", out var idxUserId) && idxUserId < row.Count)
      int.TryParse(row[idxUserId], out userId);

    return new TransactionDto
    {
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
    if (string.IsNullOrEmpty(line)) return result;

    var sb = new StringBuilder();
    bool inQuotes = false;

    for (int i = 0; i < line.Length; i++)
    {
      var c = line[i];
      if (c == '"')
      {
        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
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
      else if (c == ',' && !inQuotes)
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
    for (int j = 0; j < result.Count; j++)
    {
      result[j] = result[j].Trim();
    }

    return result;
  }
}