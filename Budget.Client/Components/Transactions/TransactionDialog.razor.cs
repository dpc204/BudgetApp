using Budget.Client.Services;
using IBudgetApiClient = Budget.Shared.Services.IBudgetApiClient;

namespace Budget.Client.Components.Transactions;

public partial class TransactionDialog
{
  [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
  [Parameter] public int InitialEnvelopeId { get; set; }
  [Parameter] public OneTransactionDetail? ExistingTransaction { get; set; }
  private MudForm? _form;
  private PurchaseHeader _header = new();
  private readonly List<TransactionDto> _lines = new();
  private List<EnvelopeDto> Envelopes = new();
  private List<BankAccountDto> Accounts = new();

  /// <summary>
  /// Stores the transaction ID when editing
  /// </summary>
  private int _transactionId = 0;

  /// <summary>
  /// Returns true if editing an existing transaction, false if adding new
  /// </summary>
  private bool IsEditMode => ExistingTransaction is not null;

  /// <summary>
  /// Button text changes based on whether we're adding or updating
  /// </summary>
  private string SaveButtonText => IsEditMode ? "Update Transaction" : "Add Transaction";

  private bool IsSaveDisabled =>
    string.IsNullOrWhiteSpace(_header.Vendor) ||
    _header.Vendor.Length > 100 ||
    _header.Date.Date > DateTime.Today ||
    !_lines.Any() ||
    _lines.Any(l => l.Amount <= 0);

  [Inject] private IBudgetApiClient Api { get; set; } = default!;
  [Inject] private IDialogService DialogService { get; set; } = default!;
  private MudTextField<string>? _vendorField;

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    await base.OnAfterRenderAsync(firstRender);
  }

  [Inject] IJSRuntime JS { get; set; }

  protected override async Task OnInitializedAsync()
  {
    if (!Envelopes.Any())
    {
      Envelopes = await Api.GetEnvelopesAsync();
    }

    if (!Accounts.Any())
    {
      Accounts = await Api.GetAccountsAsync();
      _header.AccountId = Accounts.Min(e => e.Id);
    }

    // If editing an existing transaction, pre-populate the form
    if (ExistingTransaction is not null)
    {
      _transactionId = ExistingTransaction.Id; // Store the transaction ID
      _header.AccountId = ExistingTransaction.AccountId;
      _header.Vendor = ExistingTransaction.Vendor;
      _header.Date = ExistingTransaction.Date;
      _header.TotalAmount = ExistingTransaction.TotalAmount;

      foreach (var detail in ExistingTransaction.Details)
      {
        _lines.Add(new TransactionDto
        {
          EnvelopeId = detail.EnvelopeId,
          Amount = detail.Amount,
          Description = detail.Description
        });
      }
      Recalc();
    }
    else if (!_lines.Any())
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

  private async Task HandleSaveAsync()
  {
    if (IsSaveDisabled) return;




    var result = new OneTransactionDetail()
    {
      Id = _transactionId, // Use stored transaction ID for updates
      AccountId = _header.AccountId,
      Vendor = _header.Vendor.Trim(),
      Date = _header.Date.Date,
      UserId = 1,
      UserName = UserOptions.User.Email,
      Details = _lines.Select((l, i) => new TransactionDto()
      {
        LineId = i + 1,
        EnvelopeId = l.EnvelopeId,
        Amount = l.Amount,
        Description = l.Description?.Trim() ?? string.Empty
      }).ToList(),
      TotalAmount = _header.TotalAmount
    };

    // Call appropriate API based on whether we're adding or updating
    List<EnvelopeDto> envelopes;
    if (IsEditMode)
    {
      envelopes = await BudgetApi.UpdateTransactionAsync(result);
    }
    else
    {
      envelopes = await BudgetApi.AddTransactionAsync(result);
    }

    // Pass the updated envelopes back to the caller (EnvelopePage)
    MudDialog.Close(DialogResult.Ok(envelopes));
  }

  private void Cancel() => MudDialog.Cancel();

  private async Task VoidTransaction()
  {
    var result = await DialogService.ShowMessageBox(
      "Confirm Void Transaction",
      $"Are you sure you want to void this transaction? This will reverse the transaction in the budget and account balances.\n\nVendor: {_header.Vendor}\nAmount: {_header.TotalAmount:C}",
      yesText: "Yes, Void Transaction",
      cancelText: "Cancel");

    if (result == true)
    {
      try
      {
        var envelopes = await BudgetApi.VoidTransactionAsync(_transactionId);
        MudDialog.Close(DialogResult.Ok(envelopes));
      }
      catch (Exception ex)
      {
        await DialogService.ShowMessageBox(
          "Error",
          $"Failed to void transaction: {ex.Message}",
          yesText: "OK");
      }
    }
  }

  private class PurchaseHeader
  {
    [Required, MaxLength(100)] public string Vendor { get; set; } = string.Empty;

    [Required] public int AccountId { get; set; }

    [Required] public DateTime Date { get; set; } = DateTime.Today;

    public decimal TotalAmount { get; set; }
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