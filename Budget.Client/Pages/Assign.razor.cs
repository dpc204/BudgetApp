using Budget.Shared.Models.Queries;

namespace Budget.Client.Pages;

public partial class Assign : ComponentBase
{
  [Inject] private EnvelopeState State { get; set; } = default!;
  [Inject] private IBudgetApiClient Api { get; set; } = default!;
  [Inject] private ILogger<EnvelopePage> Logger { get; set; } = default!;
  [Inject] private IUserAndOptions UserOptions { get; set; } = default!;


  public List<TransactionDto> Transactions { get; set; } = [];
  public MudDataGrid<TransactionDto> Grid { get; set; }
  public int ProgressValue { get; set; }
  public int ProgressMax { get; set; }

  private List<EnvelopeIdName> _availableEnvelopes = [];


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
  private bool Busy = true;
  private string? _loadError;
  private bool _afterRenderInit;

  protected override async Task OnInitializedAsync()
  {
    try
    {
      State.InOnInitializedAsync = true;
      await State.RefreshAsync();


      // Convert State.AllEnvelopeData to EnvelopeIdName list
      _availableEnvelopes = SetAvailableEnvelopes();

      _unassignedEnvelope = State.AllEnvelopeData.FirstOrDefault(a => a.EnvelopeType == EnvelopeTypes.Unassigned);

      var result = await Api.GetTransactionsUnassignedAsync();
      if (result.IsSuccess)
      {
        Transactions = result.Value;
      }
      else
      {
        _loadError = string.Join(", ", result.Errors.Select(e => e.Message));
        Logger.LogError("Failed to load unassigned transactions: {Errors}", _loadError);
      }
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
      Busy = false;
      await InvokeAsync(StateHasChanged);
      _loading = false;
    }
  }

  private List<EnvelopeIdName> SetAvailableEnvelopes()
  {
    return State.AllEnvelopeData
      .Where(a => a.EnvelopeType == EnvelopeTypes.Standard || a.EnvelopeType == EnvelopeTypes.Income)
      .Select(a =>
        new EnvelopeIdName(a.EnvelopeId, a.CategoryName, a.EnvelopeName, a.CategorySortOrder, a.EnvelopeSortOrder))
      .OrderBy(a => a.CategorySortOrder).ThenBy(a => a.EnvelopeSortOrder).ToList();
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
      _availableEnvelopes = SetAvailableEnvelopes();

      StateHasChanged();
    }
  }

  private string? GetEnvelopeNameOnly(EnvelopeIdName? e)
  {
    if (e == null)
      return null;

    return e.EnvelopeName;
  }

  private string? GetCatAndEnvName(EnvelopeIdName? e)
  {
    if (e == null)
      return null;

    return e.CategoryName + " - " + e.EnvelopeName;
  }

  private EnvelopeIdName? GetCurrentEnvelope(TransactionDto transaction)
  {
    return null;
  }

  bool CaseInsensitiveContains(string? source, string? search)
  {
    if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(search))
      return false;

    return source.Contains(search, StringComparison.OrdinalIgnoreCase);
  }

  private async Task<GridData<TransactionDto>> LoadServerData(GridState<TransactionDto> state,
    CancellationToken cancellationToken)
  {
    try
    {
      Logger.LogInformation(
        "Loading server data: Page={Page}, PageSize={PageSize}, Sort={Sort}, Descending={Descending}, Filters={Filters}",
        state.Page, state.PageSize,
        state.SortDefinitions.FirstOrDefault()?.SortBy,
        state.SortDefinitions.FirstOrDefault()?.Descending ?? false,
        string.Join(";", state.FilterDefinitions.Select(f => $"{f.Column?.PropertyName} {f.Operator} {f.Value}"))
      );

      var query = new AssignQuery
      {
        StartIndex = state.Page * state.PageSize,
        Count = state.PageSize,
        Sort = state.SortDefinitions.FirstOrDefault()?.SortBy,
        Descending = state.SortDefinitions.FirstOrDefault()?.Descending ?? false,
        Filters = state.FilterDefinitions
          .Select(f => new FilterItem
          {
            Column = f.Column?.PropertyName,
            Operator = f.Operator,
            Value = f.Value?.ToString()
          })
          .ToList()
      };

      var response = await Api.GetUnassignedVirtualAsync(query);

      Logger.LogInformation("Loading server data: Received {ItemCount} items, Total: {TotalCount}",
        response.Items.Count, response.TotalCount);

      return new GridData<TransactionDto>
      {
        Items = response.Items,
        TotalItems = response.TotalCount
      };
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error loading server data");
      return new GridData<TransactionDto>
      {
        Items = [],
        TotalItems = 0
      };
    }
  }


  private async Task OnEnvelopeSelectedAsync(TransactionDto transaction, EnvelopeIdName? selectedEnvelope)
  {
    if (selectedEnvelope is null) return;

    // Update the transaction's envelope
    transaction.EnvelopeId = selectedEnvelope.EnvelopeId;
    transaction.EnvelopeName = selectedEnvelope.EnvelopeName;

    // Call API to save the transaction envelope assignment
    await Api.AssignTransactionAsync(transaction.TransactionId, transaction.LineId, transaction.EnvelopeId,
      transaction.Description);
    await Grid.ReloadServerData();
    StateHasChanged();
  }

  private async Task<IEnumerable<EnvelopeIdName>> SearchEnvelopes(string? arg1, CancellationToken arg2)
  {
    if (string.IsNullOrWhiteSpace(arg1))
    {
      return [.. _availableEnvelopes];
    }

    return
    [
      .. _availableEnvelopes.Where(e =>
        e.CategoryName.Contains(arg1, StringComparison.InvariantCultureIgnoreCase) ||
        e.EnvelopeName.Contains(arg1, StringComparison.InvariantCultureIgnoreCase)
      )
    ];
  }

  private async Task<object> OnEnvelopeChanged(TransactionDto contextItem, EnvelopeIdName? val)
  {
    if (val is null) return contextItem;

    var selectedEnvelope = _availableEnvelopes.FirstOrDefault(a => a.EnvelopeId == val.EnvelopeId);

    if (selectedEnvelope is null) return contextItem;

    await OnEnvelopeSelectedAsync(contextItem, selectedEnvelope);
    return contextItem;
  }

  private async Task<object> SetBulkEnvelope(TransactionDto contextItem, EnvelopeIdName? val)
  {
    if (val is null) return contextItem;

    var selectedEnvelope = _availableEnvelopes.FirstOrDefault(a => a.EnvelopeId == val.EnvelopeId);


    if (selectedEnvelope is null) return contextItem;

    await OnEnvelopeSelectedAsync(contextItem, selectedEnvelope);
    return contextItem;
  }

  private async Task OnDescriptionChanged(TransactionDto transaction, string newDescription)
  {
    // Update the transaction's description
    transaction.Description = newDescription;

    // Call API to save the transaction description assignment
    await Api.AssignTransactionAsync(transaction.TransactionId, transaction.LineId, transaction.EnvelopeId,
      transaction.Description);

    StateHasChanged();
  }

  /// <summary>
  /// Handles the selection of an envelope for bulk assignment
  /// </summary>
  private Task OnBulkEnvelopeSelected(EnvelopeIdName? selectedEnvelope)
  {
    _bulkEnvelope = selectedEnvelope;
    return Task.CompletedTask;
  }

  private async Task BulkAssignAsync()
  {
    if (_bulkEnvelope is null || _selectedTransactions.Count == 0)
    {
      return;
    }

    try
    {

      Busy = true;
      // Loop through selected transactions and assign each one
      var transactionsToAssign = _selectedTransactions.ToList();
      ProgressMax = transactionsToAssign.Count;
      ProgressValue = 0;
      foreach(var transaction in transactionsToAssign)
      {
        ProgressValue++;
        StateHasChanged();
        transaction.EnvelopeId = _bulkEnvelope.EnvelopeId;
        transaction.EnvelopeName = _bulkEnvelope.EnvelopeName;

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
      await Grid.ReloadServerData();
      StateHasChanged();
    }
    finally
    {
      Busy = false;
    }
  }

  private int _selectedCount;
  private HashSet<TransactionImportDto> _selectedItems = new();

  private string _transactionSearch = string.Empty;
  private EnvelopeResult? _unassignedEnvelope;

  private void OnSelectedItemsChanged(HashSet<TransactionImportDto> items)
  {
    _selectedItems = items;
    _selectedCount = items.Count;
  }

  private bool FilterTransactions(TransactionDto transaction, string search)
  {
    if (string.IsNullOrWhiteSpace(search))
      return true;

    // Use case-insensitive comparison across relevant string fields
    return (transaction.Vendor ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)
           || (transaction.Description ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase);
  }
}