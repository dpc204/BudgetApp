using ITransactionsApiClient = Budget.Shared.Services.ITransactionsApiClient;

namespace Budget.Client.Components.Transactions;

public partial class EditTransactionDialog
{
  [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
  [Parameter] public int InitialEnvelopeId { get; set; }
  [Parameter] public OneTransactionDetail? ExistingTransaction { get; set; }
  [Parameter] public bool IsReadOnly { get; set; }
  private MudForm? _form;
  private readonly PurchaseHeader _header = new();
  private readonly List<TransactionDto> _lines = [];
  private List<EnvelopeDto> Envelopes = [];
  private List<BankAccountDto> Accounts = [];

  //private bool EditNotAllowed
  //{
  //  get
  //  {
  //    if (!IsEditMode)
  //      return false;
      
  //    UserOptions.IsAdminUser() == false || IsEditMode;
  //  }
  //}

  

  /// <summary>
  /// Stores the transaction ID when editing
  /// </summary>
  private int _transactionId = 0;

  /// <summary>
  /// Returns true if editing an existing transaction, false if adding new
  /// </summary>
  private bool IsEditMode => ExistingTransaction is not null;

  /// <summary>
  /// Returns true if the transaction is voided
  /// </summary>
  private bool IsVoided => ExistingTransaction?.IsVoided ?? false;

  /// <summary>
  /// Button text changes based on whether we're adding or updating
  /// </summary>
  private string SaveButtonText => IsEditMode ? "Update Transaction" : "Add Transaction";

  private bool IsSaveDisabled =>
    string.IsNullOrWhiteSpace(_header.Vendor) ||
    _header.Vendor.Length > 100 ||
    _header.Date.Date > DateTime.Today ||
    _lines.Count == 0 ||
    _lines.Any(l => l.Amount <= 0) ||
    IsBusy;

  public bool IsBusy { get; set; }

  [Inject] private ISnackbar SnackBar { get; set; } = default!;
  [Inject] private ITransactionsApiClient TransactionApi { get; set; } = default!;
  [Inject] private IEnvelopesApiClient EnvelopesApi { get; set; } = default!;
  [Inject] private IAccountsApiClient AccountsApi { get; set; } = default!;
  [Inject] private IDialogService DialogService { get; set; } = default!;
  private MudTextField<string>? _vendorField;
  private MudTextField<string>? _descriptionField;

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    await base.OnAfterRenderAsync(firstRender);
  }


  protected override async Task OnInitializedAsync()
  {
    if (Envelopes.Count == 0)
    {
      Envelopes = await EnvelopesApi.GetEnvelopesAsync();
    }

    if (Accounts.Count == 0)
    {
      Accounts = await AccountsApi.GetAccountsAsync();
      _header.AccountId = Accounts.Min(e => e.Id);
    }

  //  NotAdmin = !UserOptions.IsAdminUser();
    
    // If editing an existing transaction, pre-populate the form
    if (ExistingTransaction is not null)
    {
      _transactionId = ExistingTransaction.Id; // Store the transaction ID
      _header.AccountId = ExistingTransaction.AccountId;
      _header.Description = ExistingTransaction.Description;
      _header.Vendor = ExistingTransaction.Vendor;
      _header.Date = ExistingTransaction.Date;
      _header.TotalAmount = ExistingTransaction.TotalAmount * -1;

      foreach (var detail in ExistingTransaction.Details)
      {
        _lines.Add(new TransactionDto
        {
          EnvelopeId = detail.EnvelopeId,
          Amount = detail.Amount * -1,
          Description = detail.Notes,
          IsVoided = ExistingTransaction.IsVoided
        });
      }

      Recalc();
    }
    else if (_lines.Count == 0)
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
    //if(!IsEditMode)
    _header.TotalAmount = _lines.Sum(l => l.Amount);
    EditTotalAmount = _lines.Sum(l => l.Amount);
    StateHasChanged();
  }

  public decimal EditTotalAmount { get; set; }

  public decimal EditDifference => EditTotalAmount - _header.TotalAmount;

  private async Task Save()
  {
    if (_form is not null)
    {
      await _form.ValidateAsync();
      if (!_form.IsValid)
        return;
    }


    await HandleSaveAsync();
  }

  private async Task HandleSaveAsync()
  {
    if (IsSaveDisabled) return;

    IsBusy = true;


    var result = new OneTransactionDetail()
    {
      Id = _transactionId, // Use stored transaction ID for updates
      AccountId = _header.AccountId,
      Vendor = _header.Vendor.Trim(),
      Date = _header.Date.Date,
      UserId = 1,
      UserName = UserOptions.User.Email!,
      Details =
      [
        .. _lines.Select((l, i) => new TransactionDetailDto()
        {
          TransactionId = _transactionId,
          EnvelopeId = l.EnvelopeId,
          Amount = l.Amount * -1,
          Notes = l.Notes?.Trim() ?? string.Empty
          // LineId intentionally left unset - will be assigned by backend
        })
      ],
      TotalAmount = _header.TotalAmount * -1
    };

    EnvelopeDeltas envDeltas = [];


    // Call appropriate API based on whether we're adding or updating
    List<EnvelopeUpdate> envelopeUpdates;

    if (IsEditMode)
    {
      envelopeUpdates = await TransactionApi.UpdateTransactionAsync(result);
    }
    else
    {
      envelopeUpdates = await TransactionApi.AddTransactionAsync(result);
      ArgumentNullException.ThrowIfNull(envDeltas);
    }
    envDeltas.AddRange(envelopeUpdates);

    // Pass the updated envelopes back to the caller (EnvelopePage)
    SnackBar.Add("Transaction Saved!", Severity.Success);

    MudDialog.Close(DialogResult.Ok(envDeltas));
  }


  private void Cancel() => MudDialog.Cancel();

  private async Task VoidTransaction()
  {
    var result = await DialogService.ShowMessageBoxAsync(
      "Confirm Void Transaction",
      $"Are you sure you want to void this transaction? This will reverse the transaction in the envelope and account balances.\n\nVendor: {_header.Vendor}\nAmount: {_header.TotalAmount:C}",
      yesText: "Yes, Void Transaction",
      cancelText: "Cancel");

    if (result == true)
    {
      try
      {
        var envelopes = await TransactionApi.VoidTransactionAsync(_transactionId);
        MudDialog.Close(DialogResult.Ok(envelopes));
      }
      catch (Exception ex)
      {
        await DialogService.ShowMessageBoxAsync(
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
    public string Description { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
  }


  private static string? ValidateAmount(decimal value)
  {
    if (value <= 0m)
      return "Amount must be greater than 0.";
    // allow at most 2 decimal places
    //if ((value * 100m) % 1m != 0m)
    //  return "Maximum of two decimal places.";
    return null;
  }
}