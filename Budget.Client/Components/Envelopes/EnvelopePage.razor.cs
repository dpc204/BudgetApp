namespace Budget.Client.Components.Envelopes;

public partial class EnvelopePage : ComponentBase
{
  [Inject] private EnvelopeState State { get; set; } = default!;
  [Inject] private IBudgetApiClient Api { get; set; } = default!;
  [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
  [Inject] private ILogger<EnvelopePage> Logger { get; set; } = default!;
  [Inject] private IDialogService DialogService { get; set; } = default!;
  [Inject] private IUserAndOptions UserOptions { get; set; } = default!;

  public List<EnvelopeResult> AllEnvelopeData => State.AllEnvelopeData ?? [];
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
      await State.RefreshAsync();

      // Ensure selection class applied on first render when an item is already selected
      await InvokeAsync(StateHasChanged);
    }
    catch (Exception ex)
    {
      _loadError = ex.Message;
      if (Logger.IsEnabled(LogLevel.Error))
      {
        Logger.LogError(ex, "Error in OnInitializedAsync");
      }
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender && !_afterRenderInit)
    {
      _afterRenderInit = true;

      try
      {

        await State.TryLoadFromCacheAsync();
        if (!State.IsLoaded)
        {
          await State.RefreshAsync();
        }

        CategoriesForSelect = GetCategoriesForSelect();
        ApplyCategorySelection();
      }
      catch (Exception ex)
      {
        _loadError = ex.Message;
        Logger.LogError(ex, "Error in OnAfterRenderAsync");
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
    var selected = SelectedCategoryId ?? 0;

    var list = new List<EnvelopeResult>();

    //var list = selected == 0
    //  ? AllEnvelopeData?.ToList() ?? []
    //  : AllEnvelopeData?.Where(a => a.CategoryId == selected).ToList() ?? [];
    if (SelectedCategoryId == 0)
    {
      list = [.. (AllEnvelopeData!.Join(CategoriesForSelect, e => e.CategoryId, c => c.CategoryId, (e, c) => e))];
    }
    else
      list = [.. AllEnvelopeData.Where(a => a.CategoryId == SelectedCategoryId).OrderBy(a => a.EnvelopeId)];
     // list = [.. AllEnvelopeData.Where(a => a.CategoryId == SelectedCategoryId).OrderBy(a => a.EnvelopeId)];

    SelectedEnvelopeData = list;

    if (_selectedEnvelope is not null && list.All(e => e.EnvelopeId != _selectedEnvelope.EnvelopeId))
    {
      // Clear selection if it's no longer in the filtered list; will also clear transactions
      SelectedEnvelope = null;
    }
  }

  private List<Cat> CategoriesForSelect { get; set; } = [];

  public int? SelectedCategoryId
  {
    get => State.SelectedCategoryId;
    set => State.SelectedCategoryId = value;
  }

  public List<Cat> GetCategoriesForSelect()
  {
    if (!UserOptions.IsAdminUser())
      return [.. State.Cats.Where(a => a.CatType != CatTypes.System).OrderBy(a => a.SortOrder)];

    return State.Cats;
  }

  private async Task CatChanged(int? value)
  {
    var selected = value ?? 0;
    SelectedCategoryId = selected;
    ApplyCategorySelection();
    await State.SaveAsync();
  }


  // Overload for MudDataGrid RowClick
  private async Task OnTransactionRowClick(DataGridRowClickEventArgs<TransactionDto> args)
  {
    if (args?.Item is null) return;

    try
    {
      var detail = await Api.GetOneTransactionDetailAsync(args.Item.TransactionId);

      if (UserOptions.IsAdminUser())
      {
        // Admin users can edit transactions via EditTransactionDialog
        var parameters = new DialogParameters { [nameof(EditTransactionDialog.ExistingTransaction)] = detail };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<EditTransactionDialog>("Edit Transaction", parameters, options);
        var result = await dialog.Result;
        if (!(result is { Canceled: true }))
        {
          // Refresh envelope data after potential edit
          if (result?.Data is List<EnvelopeDto> envResult)
          {
            UpdateEnvelopeBalances(envResult);
            ApplyCategorySelection();
            await InvokeAsync(StateHasChanged);

            if (SelectedEnvelope is not null)
            {
              await OnSelectedEnvelopeChangedAsync(SelectedEnvelope);
            }
          }
        }
      }
      else
      {
        // Non-admin users see read-only ShowOneTransaction dialog
        var parameters = new DialogParameters { [nameof(ShowOneTransaction.Transaction)] = detail };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        await DialogService.ShowAsync<ShowOneTransaction>("Transaction Details", parameters, options);
      }
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Failed loading transaction detail: {ex.Message}");
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

    try
    {
      var rslt = await Api.GetTransactionsByEnvelopeAsync(envelope.EnvelopeId);
      TransactionData = [.. rslt];
      await InvokeAsync(StateHasChanged);
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Failed loading transactions: {ex.Message}");
      TransactionData = [];
      await InvokeAsync(StateHasChanged);
    }
  }

  private async Task NewTransactionAsync(EnvelopeResult? envelope)
  {
    if(envelope is null)
    {
      Logger.Log(LogLevel.Debug,$"envelope parameter is null.  Transaction cannot be added");
      return;
    }
    var parameters = new DialogParameters { [nameof(EditTransactionDialog.InitialEnvelopeId)] = envelope.EnvelopeId };
    var options = new DialogOptions
      { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
    var dialog = await DialogService.ShowAsync<EditTransactionDialog>("New Purchase", parameters, options);
    var result = await dialog.Result;
    if (!(result is { Canceled: true }))
    {
      try
      {
        // If dialog returned updated envelope DTOs, we can update state directly

        if (result?.Data is List<EnvelopeDto> envResult)
        {
          UpdateEnvelopeBalances(envResult); // or merge if there's a method; keeping simple by refresh
          ApplyCategorySelection();
          await InvokeAsync(StateHasChanged);

          EnvelopeResult er = new EnvelopeResult() { EnvelopeId = envelope.EnvelopeId };
          await OnSelectedEnvelopeChangedAsync(er);
        }
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Refresh after new purchase failed: {ex.Message}");
      }
    }
  }

  private void UpdateEnvelopeBalances(List<EnvelopeDto> envelopes)
  {
    foreach (var env in envelopes)
    {
      // Find the matching EnvelopeResult by EnvelopeId
      var rec = State.AllEnvelopeData?.Find(e => e.EnvelopeId == env.Id);
      rec?.Balance = env.Balance;

      // You can update properties here if EnvelopeResult is mutable, or handle as needed
      // Example: if EnvelopeResult is immutable, you may need to replace the item in the list
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