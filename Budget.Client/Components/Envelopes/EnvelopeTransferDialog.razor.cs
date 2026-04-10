namespace Budget.Client.Components.Envelopes;

/// <summary>
/// Dialog for transferring a balance from one envelope to another.
/// </summary>
public partial class EnvelopeTransferDialog
{
  [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

  [Inject] private ITransactionsApiClient TransactionApi { get; set; } = default!;
  [Inject] private ISnackbar SnackBar { get; set; } = default!;

  private MudForm? _form;
  private string _reason = string.Empty;
  private EnvelopeIdName? _fromEnvelope;
  private EnvelopeIdName? _toEnvelope;
  private decimal _amount;
  private string? _errorMessage;
  private bool _isBusy;

  /// <summary>
  /// Returns true when the Transfer button should be disabled.
  /// </summary>
  private bool IsTransferDisabled =>
    string.IsNullOrWhiteSpace(_reason) ||
    _fromEnvelope is null ||
    _toEnvelope is null ||
    _fromEnvelope.EnvelopeId == _toEnvelope.EnvelopeId ||
    _amount <= 0 ||
    _isBusy;

  private void Cancel() => MudDialog.Cancel();

  private void NormalizeAmount()
  {
    var v = Math.Round(_amount < 0 ? 0 : _amount, 2, MidpointRounding.AwayFromZero);
    if(v != _amount)
      _amount = v;
  }

  private static string? ValidateAmount(decimal value)
  {
    if(value <= 0m)
      return "Amount must be greater than 0.";
    return null;
  }

  /// <summary>
  /// Executes the envelope balance transfer and closes the dialog on success.
  /// </summary>
  private async Task TransferAsync()
  {
    if(IsTransferDisabled) return;

    _isBusy = true;
    _errorMessage = null;

    try
    {
      var envUpdates = await TransactionApi.TransferEnvelopeFundsAsync(
        _reason,
        _fromEnvelope!.EnvelopeId,
        _toEnvelope!.EnvelopeId,
        _amount);



      if(envUpdates != null)
      {

        SnackBar.Add("Transfer complete!", Severity.Success);
        MudDialog.Close(DialogResult.Ok(envUpdates));
      }
      else
      {
        _errorMessage = "Transfer failed. Please try again.";
      }
    }
    catch(Exception ex)
    {
      _errorMessage = $"An error occurred: {ex.Message}";
    }
    finally
    {
      _isBusy = false;
    }
  }
}
