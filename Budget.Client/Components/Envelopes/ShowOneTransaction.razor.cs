namespace Budget.Client.Components.Envelopes;

public partial class ShowOneTransaction
{
  [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

  [Parameter] public OneTransactionDetail? Transaction { get; set; }

  private void Close() => MudDialog?.Cancel();
}