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
  private EnvelopeIdName? _fromEnvelope;
  private EnvelopeIdName? _toEnvelope;
  private decimal? _amount;
  private string? _errorMessage;
  private bool _isBusy;

  /// <summary>
  /// Returns true when the Transfer button should be disabled.
  /// </summary>
  private bool IsTransferDisabled =>
    _fromEnvelope is null ||
    _toEnvelope is null ||
    _fromEnvelope.EnvelopeId == _toEnvelope.EnvelopeId ||
    _amount is null ||
    _amount <= 0 ||
    _isBusy;

  private void Cancel() => MudDialog.Cancel();

  /// <summary>
  /// Executes the envelope balance transfer and closes the dialog on success.
  /// </summary>
  private async Task TransferAsync()
  {
    if (IsTransferDisabled) return;

    _isBusy = true;
    _errorMessage = null;

    try
    {
      var success = await TransactionApi.TransferEnvelopeFundsAsync(
        _fromEnvelope!.EnvelopeId,
        _toEnvelope!.EnvelopeId,
        _amount!.Value);

      if (success)
      {
        SnackBar.Add("Transfer complete!", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
      }
      else
      {
        _errorMessage = "Transfer failed. Please try again.";
      }
    }
    catch (Exception ex)
    {
      _errorMessage = $"An error occurred: {ex.Message}";
    }
    finally
    {
      _isBusy = false;
    }
  }
}
