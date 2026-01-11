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
      _ = OnSelectedEnvelopeChangedAsync(value);
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

  private void OnRowClicked(DataGridRowClickEventArgs<EnvelopeResult> args)
  {
    if (args?.Item is null) return;
    SelectedEnvelope = args.Item;
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

  public string? SelectedCategoryId
  {
    get => state.SelectedCategoryId;
    set => state.SelectedCategoryId = value;
  }

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
  private async Task OnTransactionRowClick(DataGridRowClickEventArgs<TransactionDto> args)
  {
    if (args?.Item is null) return;

    var result = await transactionService.ShowTransactionDetailsAsync(args.Item.TransactionId);
    
    if (result?.WasEdited == true)
    {
      // Refresh envelope data after edit
      dataService.UpdateEnvelopeBalances(result.UpdatedEnvelopes);
      ApplyCategorySelection();
      await dataService.RefreshAsync();
      await InvokeAsync(StateHasChanged);

      if (SelectedEnvelope is not null)
      {
        await OnSelectedEnvelopeChangedAsync(SelectedEnvelope);
      }
    }
  }

  private void OnEnvelopeRowClick(TableRowClickEventArgs<EnvelopeResult> args)
  {
    if (args?.Item is null) return;
    SelectedEnvelope = args.Item;
  }

  private async Task OnSelectedEnvelopeChangedAsync(EnvelopeResult? envelope)
  {
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
        dataService.UpdateEnvelopeBalances(result.UpdatedEnvelopes);
        ApplyCategorySelection();
        await InvokeAsync(StateHasChanged);

        EnvelopeResult er = new EnvelopeResult() { EnvelopeId = envelope.EnvelopeId };
        await OnSelectedEnvelopeChangedAsync(er);
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
}