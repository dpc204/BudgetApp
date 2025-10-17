namespace Budget.Client.Components.Transactions;

public partial class PurchaseTransactionDialog
{
  [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
  [Parameter] public int InitialEnvelopeId { get; set; }
  private MudForm? _form;
  private PurchaseHeader _header = new();
  private readonly List<TransactionDto> _lines = new();
  private List<EnvelopeDto> Envelopes = new();
  private List<BankAccountDto> Accounts = new();

  private bool IsSaveDisabled =>
    string.IsNullOrWhiteSpace(_header.Vendor) ||
    _header.Vendor.Length > 100 ||
    _header.Date.Date > DateTime.Today ||
    !_lines.Any() ||
    _lines.Any(l => l.Amount <= 0);

  [Inject] private IBudgetApiClient Api { get; set; } = default!;
  private MudTextField<string>? _vendorField;

  protected override async  Task OnAfterRenderAsync(bool firstRender)
  {
    await base.OnAfterRenderAsync(firstRender);

 
  }
  [Inject] IJSRuntime JS { get; set; }
  protected override async Task OnInitializedAsync()
  {
    if(!Envelopes.Any())
    {
      Envelopes = await Api.GetEnvelopesAsync();
    }

    if(!Accounts.Any())
    {
      Accounts = await Api.GetAccountsAsync();
      _header.AccountId = Accounts.Min(e => e.Id);
    }

    if(!_lines.Any())
    {
      _lines.Add(new TransactionDto() { EnvelopeId = InitialEnvelopeId, Amount = 0 });
      Recalc();
    }
  }

  private DateTime? HeaderDate
  {
    get => _header.Date;
    set
    {
      if (value.HasValue)
        _header.Date = value.Value;
      StateHasChanged();
    }
  }

  private void AddLine()
  {
    _lines.Add(new TransactionDto { EnvelopeId = InitialEnvelopeId });
    Recalc();
  }

  private void DeleteLine(TransactionDto line)
  {
    _lines.Remove(line);
    Recalc();
  }

  private void NormalizeAmount(TransactionDto line)
  {
    // Clamp to >= 0 and round to 2 decimals
    var v = Math.Round(line.Amount < 0 ? 0 : line.Amount, 2, MidpointRounding.AwayFromZero);
    if (v != line.Amount)
      line.Amount = v;
    Recalc();
  }

  private void Recalc()
  {
    _header.TotalAmount = _lines.Sum(l => l.Amount);
    StateHasChanged();
  }

  private async Task Save()
  {
    if (_form is not null)
    {
      await _form.Validate();
      if (!_form.IsValid)
        return;
    }

    await HandleSaveAsync();
  }

  private Task HandleSaveAsync()
  {
    if (IsSaveDisabled) return Task.CompletedTask;

    var result = new OneTransactionDetail()
    {
      AccountId = _header.AccountId,
      Vendor = _header.Vendor.Trim(),
      Date = _header.Date.Date,
      UserId = 1,
      Details = _lines.Select((l, i) => new TransactionDto()
      {
        LineId = i + 1,
        EnvelopeId = l.EnvelopeId,
        Amount = l.Amount,
        Description = l.Description?.Trim() ?? string.Empty
      }).ToList(),
      TotalAmount = _header.TotalAmount
    };

    BudgetApi.AddTransactionAsync(result);

    MudDialog.Close(DialogResult.Ok(result));
    return Task.CompletedTask;
  }

  private decimal Allowance { get; set; } = 0.00m;

  private string AllowanceString
  {
    get => Allowance.ToString("0.00");
    set
    {
      if (decimal.TryParse(value, out var result))
        Allowance = Math.Truncate(result * 100) / 100m;
    }
  }

  private Dictionary<string, object> inputAttributes = new()
  {
    { "oninput", "limitDecimalPlaces(this, 2)" }
  };

  private void Cancel() => MudDialog.Cancel();

  private class PurchaseHeader
  {
    [Required, MaxLength(100)]
    public string Vendor { get; set; } = string.Empty;

    [Required]
    public int AccountId { get; set; }
    public string AccountName { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    public decimal TotalAmount { get; set; }
  }

  private class PurchaseLine
  {
    public int EnvelopeId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Note { get; set; }
  }

 

  private string? ValidateAmount(decimal value)
  {
    if (value <= 0m)
      return "Amount must be greater than 0.";
    // allow at most 2 decimal places
    if ((value * 100m) % 1m != 0m)
      return "Maximum of two decimal places.";
    return null;
  }
}