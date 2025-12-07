namespace Budget.Client.Components.Envelopes;

public partial class TransactionAssign : ComponentBase
{
  [Inject] private EnvelopeState State { get; set; } = default!;
  [Inject] private IBudgetApiClient Api { get; set; } = default!;
  [Inject] private ILogger<EnvelopePage> Logger { get; set; } = default!; 
  [Inject] private IUserAndOptions UserOptions { get; set; } = default!; 


  public List<TransactionDto> Transactions { get; set; } = [];
  private Dictionary<int, EnvelopeIdName> _availableEnvelopes = [];


  // Multi-selection stat
  private HashSet<TransactionDto> _selectedTransactions = [];
  private EnvelopeIdName? _bulkEnvelope;

  // Height calculation so the MudDataGrid shows exactly 3 rows and scrolls for more
  // Dense row height in MudBlazor is ~33px; header is ~56px. Adjust if theme differs.
  private const int EnvelopeRowHeightPx = 38;
  private const int EnvelopeHeaderHeightPx = 56;
  private static string EnvelopeGridHeightPx => $"{(EnvelopeRowHeightPx * 5) + EnvelopeHeaderHeightPx}px";

  private const int TransactionRowHeightPx = 38;
  private const int TransactionHeaderHeightPx = 56;
  private static string TransactionGridHeightPx => $"{(TransactionRowHeightPx * 5) + TransactionHeaderHeightPx}px";

  private bool _loading = true;
  private string? _loadError;
  private bool _afterRenderInit;

  protected override async Task OnInitializedAsync()
  {
    try
    {
      await State.RefreshAsync();

      // Convert State.AllEnvelopeData to EnvelopeIdName list
      _availableEnvelopes =
        State.AllEnvelopeData?.ToDictionary(e => e.EnvelopeId, e => new EnvelopeIdName(e.EnvelopeId, e.EnvelopeName)) ??
        [];

      Transactions = await Api.GetTransactionsUnassignedAsync();
    }
    catch (Exception ex)
    {
      _loadError = ex.Message;
      if (Logger.IsEnabled(LogLevel.Error))
      {
        Logger.LogError(ex, "Error in OnInitializedAsync");
      }
    }
    finally
    {
      _loading = false;
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender && !_afterRenderInit)
    {
      _afterRenderInit = true;
      await State.TryLoadFromCacheAsync();
      if (!State.IsLoaded)
      {
        await State.RefreshAsync();
      }

      // Refresh envelope list after state is loaded
      _availableEnvelopes =
        State.AllEnvelopeData?.ToDictionary(e => e.EnvelopeId, x => new EnvelopeIdName(x.EnvelopeId, x.EnvelopeName)) ??
        [];


      StateHasChanged();
    }
  }

  private static void OnRowClicked(DataGridRowClickEventArgs<TransactionDto> args)
  {
    if (args?.Item is null) return;
  }

  private EnvelopeIdName? GetCurrentEnvelope(TransactionDto transaction)
  {
    return _availableEnvelopes.GetValueOrDefault(transaction.EnvelopeId);
  }

  private async Task OnEnvelopeSelectedAsync(TransactionDto transaction, EnvelopeIdName? selectedEnvelope)
  {
    if (selectedEnvelope is null) return;

    // Update the transaction's envelope
    transaction.EnvelopeId = selectedEnvelope.Id;
    transaction.EnvelopeName = selectedEnvelope.Name;

    // Call API to save the transaction envelope assignment
    await Api.AssignTransactionAsync(transaction.TransactionId, transaction.LineId, transaction.EnvelopeId, transaction.Description);

    StateHasChanged();
  }

  private async Task<IEnumerable<EnvelopeIdName>> SearchEnvelopes(string? arg1, CancellationToken arg2)
  {
    if (string.IsNullOrWhiteSpace(arg1))
    {
      return [.. _availableEnvelopes.Values];
    }

    return [.. _availableEnvelopes.Values.Where(e =>
      e.Name.Contains(arg1, StringComparison.InvariantCultureIgnoreCase))];
  }

  private async Task<object> OnEnvelopeChanged(TransactionDto contextItem, EnvelopeIdName? val)
  {
    if (val is null) return contextItem;

    var selectedEnvelope = _availableEnvelopes.GetValueOrDefault(val.Id);

    if (selectedEnvelope is null) return contextItem;

    await OnEnvelopeSelectedAsync(contextItem, selectedEnvelope);
    return contextItem;
  }

  private async Task OnDescriptionChanged(TransactionDto transaction, string newDescription)
  {
    // Update the transaction's description
    transaction.Description = newDescription;

    // Call API to save the transaction description assignment
    await Api.AssignTransactionAsync(transaction.TransactionId, transaction.LineId, transaction.EnvelopeId, transaction.Description);

    StateHasChanged();
  }

  private async Task BulkAssignAsync()
  {
    if (_bulkEnvelope is null || _selectedTransactions.Count == 0)
    {
      return;
    }

    // Loop through selected transactions and assign each one
    var transactionsToAssign = _selectedTransactions.ToList();
    foreach (var transaction in transactionsToAssign)
    {
      transaction.EnvelopeId = _bulkEnvelope.Id;
      transaction.EnvelopeName = _bulkEnvelope.Name;

      await Api.AssignTransactionAsync(
        transaction.TransactionId,
        transaction.LineId,
        transaction.EnvelopeId,
        transaction.Description);

      // Remove from the unassigned transactions list
      Transactions.Remove(transaction);
    }

    // Clear selection after assignment
    _selectedTransactions.Clear();
    _bulkEnvelope = null;

    StateHasChanged();
  }
}
