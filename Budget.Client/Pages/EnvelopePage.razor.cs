namespace Budget.Client.Pages;

public partial class EnvelopePage(
  IEnvelopeDataService dataService,
  IEnvelopeTransactionService transactionService,
  EnvelopeState state,
  ILogger<EnvelopePage> logger) : ComponentBase
{
  [CascadingParameter] private IUserAndOptions? UserOptions { get; set; }

  private List<EnvelopeResult> AllEnvelopeData { get; set; } = [];
  private List<EnvelopeResult> SelectedEnvelopeData { get; set; } = [];

  private EnvelopeResult? _selectedEnvelope;

  private EnvelopeResult? SelectedEnvelope
  {
    get => _selectedEnvelope;
    set
    {
      if(ReferenceEquals(_selectedEnvelope, value)) return;
      _selectedEnvelope = value;

      // Use InvokeAsync to ensure we're on the UI thread and queue the async work
      //  InvokeAsync(async () => await OnSelectedEnvelopeChangedAsync(value));
    }
  }

  private bool _loading = true;
  private string? _loadError;
  private bool _afterRenderInit;

  // Counter to force child grids to refresh by changing their @key
  private int _childGridRefreshKey;



  protected override async Task OnInitializedAsync()
  {
    try
    {
      state.InOnInitializedAsync = true;
      await state.RefreshAsync();
      //    await UserOptions.GetUserAsync();
      // Ensure selection class applied on first render when an item is already selected
      await InvokeAsync(StateHasChanged);
    }
    catch(Exception ex)
    {
      _loadError = ex.Message;
      if(logger.IsEnabled(LogLevel.Error))
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
    if(firstRender && !_afterRenderInit)
    {
      _afterRenderInit = true;

      try
      {
        var result = await dataService.LoadEnvelopeDataAsync();

        AllEnvelopeData = result.AllEnvelopes;
        SelectedCategoryId = result.SelectedCategoryId;
        CategoriesForSelect = result.Categories;
        ApplyCategorySelection();
        await CatChanged(SelectedCategoryId);
      }
      catch(Exception ex)
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




  private void ApplyCategorySelection()
  {
    SelectedEnvelopeData = [.. dataService.ApplyCategoryFilter(
      AllEnvelopeData,
      CategoriesForSelect,
      SelectedCategoryId)
      .Where(a=> a.EnvelopeType == EnvelopeTypes.Standard)];

    if(_selectedEnvelope is not null && SelectedEnvelopeData.All(e => e.EnvelopeId != _selectedEnvelope.EnvelopeId))
    {
      // Clear selection if it's no longer in the filtered list; will also clear transactions
      SelectedEnvelope = null;
    }
  }

  private List<Cat> CategoriesForSelect { get; set; } = [];

  public string? SelectedCategoryId { get; set; }

  public MudDataGrid<EnvelopeResult> EnvGrid { get; set; } = null!;

  private async Task CatChanged(string? value)
  {
    var selected = value ?? "0";
    SelectedCategoryId = selected;
    UserOptions?.Options.SelectedCategoryType = selected;
    ApplyCategorySelection();
    await dataService.SaveStateAsync();
  }




  // Overload for MudDataGrid RowClick
  private async Task OnTransactionRowClick(EnvelopeTransactionListItem lineItem, bool readOnly = true)
  {

    var result = await transactionService.ShowTransactionDetailsAsync(lineItem.TransactionId, readOnly);


    if(result?.WasEdited == true)
    {
      // Refresh envelope data after edit
      dataService.UpdateClientSideEnvelopeBalances(result.Deltas, AllEnvelopeData);
      ApplyCategorySelection();
      await dataService.RefreshAsync();

      // Increment refresh key to force child grids to reload
      _childGridRefreshKey++;

      await InvokeAsync(StateHasChanged);
    }
  }

  private async Task NewTransactionAsync(EnvelopeResult? envelope)
  {
    if(envelope is null)
    {
      logger.Log(LogLevel.Debug, "envelope parameter is null. Transaction cannot be added");
      return;
    }

    var result = await transactionService.ShowNewTransactionDialogAsync(envelope.EnvelopeId);

    if(result?.WasEdited == true)
    {
      try
      {
        await UpdateClientBalances(result.Deltas);
      }
      catch(Exception ex)
      {
        await Console.Error.WriteLineAsync($"Refresh after new purchase failed: {ex.Message}");
      }
    }
  }

  private async Task UpdateClientBalances(EnvelopeDeltas deltas)
  {
    dataService.UpdateClientSideEnvelopeBalances(deltas, AllEnvelopeData);
    ApplyCategorySelection();

    // Increment refresh key to force child grids to reload
    _childGridRefreshKey++;

    await InvokeAsync(StateHasChanged);
  }

  private string? GetEnvelopeRowClass(EnvelopeResult? item, int rowNumber)
  {
    if(item == null)
      return null;

    return SelectedEnvelope?.EnvelopeId == item.EnvelopeId ? "row-selected-secondary" : string.Empty;
  }

  private string? GetEnvelopeRowStyle(EnvelopeResult? item, int rowNumber)
  {
    if(item == null)
      return null;

    return SelectedEnvelope?.EnvelopeId == item.EnvelopeId
      ? "background-color: var(--mud-palette-gray-dark); color: var(--mud-palette-secondary-contrastText);"
      : string.Empty;
  }

  //private async Task<GridData<EnvelopeTransactionListItem>> ServerDataForEnvelope(
  //  EnvelopeResult? envelope,
  //  GridState<EnvelopeTransactionListItem> state,
  //  CancellationToken token)
  //{
  //  try
  //  {
  //    // Check if cancelled before starting
  //    if (token.IsCancellationRequested)
  //    {
  //      logger.LogDebug("ServerData request cancelled before starting for envelope {EnvelopeId}", envelope?.EnvelopeId);
  //      return new GridData<EnvelopeTransactionListItem> { Items = [], TotalItems = 0 };
  //    }

  //    var envelopeId = envelope?.EnvelopeId ?? 0;
  //    if (envelopeId == 0)
  //    {
  //      return new GridData<EnvelopeTransactionListItem> { Items = [], TotalItems = 0 };
  //    }

  //    var transactions = await transactionService.LoadFullTransactionsAsync(
  //      envelopeId,
  //      state.Page * state.PageSize,
  //      state.PageSize,
  //      token);

  //    // Map TransactionDto to TransactionResult if needed, or return the actual data
  //    return new GridData<EnvelopeTransactionListItem>
  //    {
  //      Items = transactions,
  //      TotalItems = transactions.Count
  //    };
  //  }
  //  catch (OperationCanceledException)
  //  {
  //    // This is expected when virtualization cancels requests
  //    logger.LogDebug("ServerData request was cancelled for envelope {EnvelopeId} (normal virtualization behavior)",
  //      envelope?.EnvelopeId);
  //    return new GridData<EnvelopeTransactionListItem> { Items = [], TotalItems = 0 };
  //  }
  //  catch (Exception ex)
  //  {
  //    logger.LogError(ex, "Error loading server data for transactions.");
  //    return new GridData<EnvelopeTransactionListItem> { Items = [], TotalItems = 0 };
  //  }
  //}

  private async Task<GridData<EnvelopeTransactionListItem>> ServerDataForEnvelopeVirtualized(
    EnvelopeResult? envelope,
    GridStateVirtualize<EnvelopeTransactionListItem> stateVirtualize,
    CancellationToken token)
  {
    try
    {
      // Check if cancelled before starting
      if(token.IsCancellationRequested)
      {
        logger.LogDebug("ServerData request cancelled before starting for envelope {EnvelopeId}", envelope?.EnvelopeId);
        return new GridData<EnvelopeTransactionListItem> { Items = [], TotalItems = 0 };
      }

      var envelopeId = envelope?.EnvelopeId ?? 0;
      if(envelopeId == 0)
      {
        return new GridData<EnvelopeTransactionListItem> { Items = [], TotalItems = 0 };
      }

      var transactions = await transactionService.LoadFullTransactionsAsync(
        envelopeId,
        stateVirtualize.StartIndex,
        stateVirtualize.Count,
        token);

      // Map TransactionDto to TransactionResult if needed, or return the actual data
      return new GridData<EnvelopeTransactionListItem> {
        Items = transactions,
        TotalItems = transactions.Count
      };
    }
    catch(OperationCanceledException)
    {
      // This is expected when virtualization cancels requests
      logger.LogDebug("ServerData request was cancelled for envelope {EnvelopeId} (normal virtualization behavior)",
        envelope?.EnvelopeId);
      return new GridData<EnvelopeTransactionListItem> { Items = [], TotalItems = 0 };
    }
    catch(Exception ex)
    {
      logger.LogError(ex, "Error loading server data for transactions.");
      return new GridData<EnvelopeTransactionListItem> { Items = [], TotalItems = 0 };
    }
  }


  private async Task OnTransferClick(MouseEventArgs args)
  {
    var deltas = await transactionService.ShowEnvelopeTransferDialogAsync();
    if(deltas != null)
    {
      dataService.UpdateClientSideEnvelopeBalances(deltas, AllEnvelopeData);
      ApplyCategorySelection();
      await dataService.RefreshAsync();

      // Increment refresh key to force child grids to reload
      _childGridRefreshKey++;
    }

  }
}