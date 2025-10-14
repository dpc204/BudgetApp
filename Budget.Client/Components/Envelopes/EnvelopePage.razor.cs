using Microsoft.Extensions.Logging;

namespace Budget.Client.Components.Envelopes;

public partial class EnvelopePage : ComponentBase
{
  [Inject] private EnvelopeState State { get; set; } = default!;
  [Inject] private IBudgetApiClient Api { get; set; } = default!;
  [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
  [Inject] private ILogger<EnvelopePage> Logger { get; set; } = default!;
  [Inject] private IDialogService DialogService { get; set; } = default!;

  public List<EnvelopeResult>? AllEnvelopeData => State.AllEnvelopeData;
  public List<EnvelopeResult>? SelectedEnvelopeData { get; set; } = [];
  public List<TransactionDto>? TransactionData { get; set; } = [];

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

  private bool _loading = true;
  private string? _loadError;
  private bool _afterRenderInit;

  protected override async Task OnInitializedAsync()
  {
    var runtimeType = JSRuntime.GetType().Name;
    Logger.LogInformation("EnvelopePage.OnInitializedAsync - Runtime: {Runtime}", runtimeType);
    Console.WriteLine($"EnvelopePage running on: {runtimeType}");

    try
    {
      await State.EnsureLoadedAsync();
      ApplySelection();
      // Ensure selection class applied on first render when an item is already selected
      await InvokeAsync(StateHasChanged);
    }
    catch (Exception ex)
    {
      _loadError = ex.Message;
      Logger.LogError(ex, "Error in OnInitializedAsync");
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender && !_afterRenderInit)
    {
      _afterRenderInit = true;
      var runtimeType = JSRuntime.GetType().Name;
      Logger.LogInformation("EnvelopePage.OnAfterRenderAsync - Runtime: {Runtime}", runtimeType);
      Console.WriteLine($"OnAfterRenderAsync running on: {runtimeType}");

      try
      {
        await State.TryLoadFromCacheAsync();
        if (!State.IsLoaded)
        {
          await State.RefreshAsync();
        }

        ApplySelection();
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

  private void ApplySelection()
  {
    var selected = SelectedCategoryId ?? 0;
    var list = selected == 0
      ? AllEnvelopeData?.ToList() ?? []
      : AllEnvelopeData?.Where(a => a.CategoryId == selected).ToList() ?? [];

    SelectedEnvelopeData = list;

    if (_selectedEnvelope is not null && !list.Any(e => e.EnvelopeId == _selectedEnvelope.EnvelopeId))
    {
      // Clear selection if it's no longer in the filtered list; will also clear transactions
      SelectedEnvelope = null;
    }
  }

  internal List<Cat> Cats => State.Cats;

  public int? SelectedCategoryId
  {
    get => State.SelectedCategoryId;
    set => State.SelectedCategoryId = value;
  }

  private async Task CatChanged(int? value)
  {
    var selected = value ?? 0;
    SelectedCategoryId = selected;
    ApplySelection();
    await State.SaveAsync();
  }

  private async Task OnTransactionRowClick(TableRowClickEventArgs<TransactionDto> args)
  {
    if (args?.Item is null) return;

    try
    {
      var detail = await Api.GetOneTransactionDetailAsync(args.Item.TransactionId);
      var parameters = new DialogParameters { [nameof(ShowOneTransaction.Transaction)] = detail };
      var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
      await DialogService.ShowAsync<ShowOneTransaction>("Transaction Details", parameters, options);
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
      TransactionData = rslt.ToList();
      await InvokeAsync(StateHasChanged);
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Failed loading transactions: {ex.Message}");
      TransactionData = [];
      await InvokeAsync(StateHasChanged);
    }
  }

  private async Task NewTransactionAsync(int envelopeId)
  {
    var parameters = new DialogParameters { [nameof(PurchaseTransactionDialog.InitialEnvelopeId)] = envelopeId };
    var options = new DialogOptions
      { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
    var dialog = await DialogService.ShowAsync<PurchaseTransactionDialog>("New Purchase", parameters, options);
    var result = await dialog.Result;
    if (!(result is { Canceled: true }))
    {
      try
      {
        EnvelopeResult er = new EnvelopeResult() { EnvelopeId = envelopeId };
        await OnSelectedEnvelopeChangedAsync(er);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Refresh after new purchase failed: {ex.Message}");
      }
    }
  }

  private string? GetEnvelopeRowClass(EnvelopeResult item, int rowNumber)
    => SelectedEnvelope?.EnvelopeId == item.EnvelopeId ? "row-selected-secondary" : null;

  private string? GetEnvelopeRowStyle(EnvelopeResult item, int rowNumber)
    => SelectedEnvelope?.EnvelopeId == item.EnvelopeId
      ? "background-color: var(--mud-palette-secondary); color: var(--mud-palette-secondary-contrastText);"
      : null;
}