using Budget.Client.Components.Envelopes;

namespace Budget.Client.Pages;

public partial class EnvelopePage(
  IEnvelopeDataService dataService,
  IEnvelopeTransactionService transactionService,
  EnvelopeState state,
  ILogger<EnvelopePage> logger) : ComponentBase
{
  [Inject] private IUserAndOptions UserOptions { get; set; } = default!;

  public List<EnvelopeResult> AllEnvelopeData => state.AllEnvelopeData ?? [];
  public List<EnvelopeResult> SelectedEnvelopeData { get; set; } = [];
  public List<TransactionDto> TransactionData { get; set; } = [];

  private EnvelopeResult? _selectedEnvelope;

  public EnvelopeResult? SelectedEnvelope
  {
    get => _selectedEnvelope;
    set
    {
      if (ReferenceEquals(_selectedEnvelope, value)) return;
      _selectedEnvelope = value;

      // Use InvokeAsync to ensure we're on the UI thread and queue the async work
      InvokeAsync(async () => await OnSelectedEnvelopeChangedAsync(value));
    }
  }

  // Height calculation so the MudDataGrid shows exactly3 rows and scrolls for more
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
  private DateTime dummy = DateTime.Now;

  // Counter to force child grids to refresh by changing their @key
  private int _childGridRefreshKey = 0;



  protected override async Task OnInitializedAsync()
  {
    try
    {
      state.InOnInitializedAsync = true;
      await state.RefreshAsync();

      // Ensure selection class applied on first render when an item is already selected
      await InvokeAsync(StateHasChanged);
    }
    catch (Exception ex)
    {
      _loadError = ex.Message;
      if (logger.IsEnabled(LogLevel.Error))
      {
        logger.LogError(ex, "Error in OnInitializedAsync");
      }
    }
    finally
    {
      state.InOnInitializedAsync = false;
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender && !_afterRenderInit)
    {
      _afterRenderInit = true;

      try
      {
        var result = await dataService.LoadEnvelopeDataAsync();

        SelectedCategoryId = result.SelectedCategoryId;
        CategoriesForSelect = result.Categories;
        ApplyCategorySelection();
        await CatChanged(SelectedCategoryId);
      }
      catch (Exception ex)
      {
        _loadError = ex.Message;
        logger.LogError(ex, "Error in OnAfterRenderAsync");
      }
      finally
      {
        _loading = false;
        StateHasChanged();
      }
    }
  }

  private void OnRowClickedWrapper(DataGridRowClickEventArgs<EnvelopeResult> args)
  {
    // This handles the synchronous row click event from MudDataGrid
    // The actual selection and data loading happens in OnSelectedItemChanged
    if (args?.Item is null) return;

    // Just set the field - don't call StateHasChanged here
    // MudDataGrid will update SelectedItem which triggers OnSelectedItemChanged
    _selectedEnvelope = args.Item;
  }

  private async Task OnSelectedItemChanged(EnvelopeResult? item)
  {
    if (item is null)
    {
      _selectedEnvelope = null;
      TransactionData = [];
      StateHasChanged();
      return;
    }

    // Avoid reloading if it's already selected
    if (ReferenceEquals(_selectedEnvelope, item)) return;

    _selectedEnvelope = item;

    // Load transactions for the selected envelope
    await OnSelectedEnvelopeChangedAsync(item);
  }


  private void ApplyCategorySelection()
  {
    SelectedEnvelopeData = dataService.ApplyCategoryFilter(
      AllEnvelopeData,
      CategoriesForSelect,
      SelectedCategoryId);

    if (_selectedEnvelope is not null && SelectedEnvelopeData.All(e => e.EnvelopeId != _selectedEnvelope.EnvelopeId))
    {
      // Clear selection if it's no longer in the filtered list; will also clear transactions
      SelectedEnvelope = null;
    }
  }

  private List<Cat> CategoriesForSelect { get; set; } = [];

  //public string? SelectedCategoryId
  //{
  //  get => state.SelectedCategoryId;
  //  set => state.SelectedCategoryId = value;
  //}
  public string? SelectedCategoryId { get; set; }
  public MudDataGrid<EnvelopeResult> EnvGrid { get; set; }

  public List<Cat> GetCategoriesForSelect()
  {
    return dataService.GetCategoriesForSelect();
  }

  private async Task CatChanged(string? value)
  {
    var selected = value ?? "0";
    SelectedCategoryId = selected;
    UserOptions.Options.SelectedCategoryType = selected;
    ApplyCategorySelection();
    await dataService.SaveStateAsync();
  }


  // Overload for MudDataGrid RowClick
  private async Task OnTransactionRowClick(EnvelopeTransactionListItem lineItem, bool readOnly=true)
  {

    var result = await transactionService.ShowTransactionDetailsAsync(lineItem.TransactionId, readOnly);
    

    if (result?.WasEdited == true)
    {
      // Refresh envelope data after edit
      dataService.UpdateClientSideEnvelopeBalances(result.UpdatedEnvelopes);
      ApplyCategorySelection();
      await dataService.RefreshAsync();
      
      // Increment refresh key to force child grids to reload
      _childGridRefreshKey++;
      
      await InvokeAsync(StateHasChanged);
    }
  }



  private async Task LoadTransactionsForRightGrid()
  {
    // DISABLED - This loads data for the RIGHT-SIDE transaction grid (line 89 in razor)
    // Keeping this method for when we re-enable the right-side grid later
    // The child grid in ChildRowContent loads its own data via ServerData

    // TODO: Re-enable once inline grid is working
    /*
    if (SelectedEnvelope is null)
    {
      TransactionData = [];
      return;
    }

    TransactionData = await transactionService.LoadTransactionsAsync(SelectedEnvelope.EnvelopeId);
    */

    await Task.CompletedTask; // Keep method async for future use
  }

  private async Task OnSelectedEnvelopeChangedAsync(EnvelopeResult? envelope)
  {
    await Task.CompletedTask;
    return;
    if (envelope is null)
    {
      TransactionData = [];
      await InvokeAsync(StateHasChanged);
      return;
    }

    TransactionData = await transactionService.LoadTransactionsAsync(envelope.EnvelopeId);
    await InvokeAsync(StateHasChanged);
  }

  private async Task NewTransactionAsync(EnvelopeResult? envelope)
  {
    if (envelope is null)
    {
      logger.Log(LogLevel.Debug, "envelope parameter is null. Transaction cannot be added");
      return;
    }

    var result = await transactionService.ShowNewTransactionDialogAsync(envelope.EnvelopeId);

    if (result?.WasEdited == true)
    {
      try
      {
        dataService.UpdateClientSideEnvelopeBalances(result.UpdatedEnvelopes);
        ApplyCategorySelection();
        
        // Increment refresh key to force child grids to reload
        _childGridRefreshKey++;
        
        await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Refresh after new purchase failed: {ex.Message}");
      }
    }
  }

  private string? GetEnvelopeRowClass(EnvelopeResult? item, int rowNumber)
  {
    if (item == null)
      return null;

    return SelectedEnvelope?.EnvelopeId == item.EnvelopeId ? "row-selected-secondary" : string.Empty;
  }

  private string? GetEnvelopeRowStyle(EnvelopeResult? item, int rowNumber)
  {
    if (item == null)
      return null;

    return SelectedEnvelope?.EnvelopeId == item.EnvelopeId
      ? "background-color: var(--mud-palette-gray-dark); color: var(--mud-palette-secondary-contrastText);"
      : string.Empty;
  }

  private async Task<GridData<EnvelopeTransactionListItem>> ServerDataForEnvelope(
    EnvelopeResult? envelope,
    GridState<EnvelopeTransactionListItem> state,
    CancellationToken token)
  {
    try
    {
      // Check if cancelled before starting
      if (token.IsCancellationRequested)
      {
        logger.LogDebug("ServerData request cancelled before starting for envelope {EnvelopeId}", envelope?.EnvelopeId);
        return new GridData<EnvelopeTransactionListItem> { Items = Array.Empty<EnvelopeTransactionListItem>(), TotalItems = 0 };
      }

      var envelopeId = envelope?.EnvelopeId ?? 0;
      if (envelopeId == 0)
      {
        return new GridData<EnvelopeTransactionListItem> { Items = Array.Empty<EnvelopeTransactionListItem>(), TotalItems = 0 };
      }

      var transactions = await transactionService.LoadFullTransactionsAsync(
        envelopeId,
        state.Page * state.PageSize,
        state.PageSize,
        token);

      // Map TransactionDto to TransactionResult if needed, or return the actual data
      return new GridData<EnvelopeTransactionListItem>
      {
        Items = transactions,
        TotalItems = transactions.Count
      };
    }
    catch (OperationCanceledException)
    {
      // This is expected when virtualization cancels requests
      logger.LogDebug("ServerData request was cancelled for envelope {EnvelopeId} (normal virtualization behavior)",
        envelope?.EnvelopeId);
      return new GridData<EnvelopeTransactionListItem> { Items = Array.Empty<EnvelopeTransactionListItem>(), TotalItems = 0 };
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error loading server data for transactions.");
      return new GridData<EnvelopeTransactionListItem> { Items = Array.Empty<EnvelopeTransactionListItem>(), TotalItems = 0 };
    }
  }

  private async Task<GridData<EnvelopeTransactionListItem>> ServerDataForEnvelopeVirtualized(
    EnvelopeResult? envelope,
    GridStateVirtualize<EnvelopeTransactionListItem> stateVirtualize,
    CancellationToken token)
  {
    try
    {
      // Check if cancelled before starting
      if (token.IsCancellationRequested)
      {
        logger.LogDebug("ServerData request cancelled before starting for envelope {EnvelopeId}", envelope?.EnvelopeId);
        return new GridData<EnvelopeTransactionListItem> { Items = Array.Empty<EnvelopeTransactionListItem>(), TotalItems = 0 };
      }

      var envelopeId = envelope?.EnvelopeId ?? 0;
      if (envelopeId == 0)
      {
        return new GridData<EnvelopeTransactionListItem> { Items = Array.Empty<EnvelopeTransactionListItem>(), TotalItems = 0 };
      }

      var transactions = await transactionService.LoadFullTransactionsAsync(
        envelopeId,
        stateVirtualize.StartIndex,
        stateVirtualize.Count,
        token);

      // Map TransactionDto to TransactionResult if needed, or return the actual data
      return new GridData<EnvelopeTransactionListItem>
      {
        Items = transactions,
        TotalItems = transactions.Count
      };
    }
    catch (OperationCanceledException)
    {
      // This is expected when virtualization cancels requests
      logger.LogDebug("ServerData request was cancelled for envelope {EnvelopeId} (normal virtualization behavior)",
        envelope?.EnvelopeId);
      return new GridData<EnvelopeTransactionListItem> { Items = Array.Empty<EnvelopeTransactionListItem>(), TotalItems = 0 };
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error loading server data for transactions.");
      return new GridData<EnvelopeTransactionListItem> { Items = Array.Empty<EnvelopeTransactionListItem>(), TotalItems = 0 };
    }
  }

 
}