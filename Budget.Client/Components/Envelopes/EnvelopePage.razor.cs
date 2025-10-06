using Budget.Client.Components.Transactions;
using Budget.Shared.Models;
using Budget.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Syncfusion.Blazor.Grids;

namespace Budget.Client.Components.Envelopes;

public partial class EnvelopePage : ComponentBase
{
  [Inject] private EnvelopeState State { get; set; } = default!;
  [Inject] private IBudgetApiClient Api { get; set; } = default!;
  [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
  [Inject] private ILogger<EnvelopePage> Logger { get; set; } = default!;

  public List<EnvelopeResult>? AllEnvelopeData => State.AllEnvelopeData;
  public List<EnvelopeResult>? SelectedEnvelopeData { get; set; } = [];
  public List<TransactionDto>? TransactionData { get; set; } = [];

  private bool ShowTransactionDialog { get; set; }
  private TransactionDto? SelectedTransaction { get; set; }
  private OneTransactionDetail? SelectedTransactionDetail { get; set; }

  private bool ShowPurchaseDialog { get; set; }
  private int InitialEnvelopeIdForNew { get; set; }
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
      await State.EnsureLoadedAsync(); // API only; no JS
      ApplySelection();
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
        await State.TryLoadFromCacheAsync(); // hydrate from localStorage if present
        if (!State.IsLoaded)
        {
          await State.RefreshAsync(); // fallback (also persists after cache attempt)
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
    SelectedEnvelopeData = selected == 0
      ? AllEnvelopeData?.ToList() ?? []
      : AllEnvelopeData?.Where(a => a.CategoryId == selected).ToList() ?? [];
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

  private async Task RecordDoubleClickHandler(RecordDoubleClickEventArgs<TransactionDto> args)
  {
    if (args?.RowData is null) return;

    SelectedTransaction = args.RowData;
    ShowTransactionDialog = true;

    try
    {
      SelectedTransactionDetail = await Api.GetOneTransactionDetailAsync(args.RowData.TransactionId);
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Failed loading transaction detail: {ex.Message}");
    }
  }

  private async Task RowSelectHandler(RowSelectEventArgs<EnvelopeResult> args)
  {
    if (args?.Data is null)
      return;

    try
    {
      var rslt = await Api.GetTransactionsByEnvelopeAsync(args.Data.EnvelopeId);
      TransactionData = rslt.ToList();
      InitialEnvelopeIdForNew = args.Data.EnvelopeId; // track for new transaction default
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Failed loading transactions: {ex.Message}");
      TransactionData = [];
    }
  }

  private void OnShowTransactionDialogChanged(bool value) => ShowTransactionDialog = value;

  private void NewTransaction(int envelopeId)
  {
    InitialEnvelopeIdForNew = envelopeId;
    ShowPurchaseDialog = true;
  }

  private void OnPurchaseDialogVisibleChanged(bool value) => ShowPurchaseDialog = value;

  private Task HandlePurchaseSaved(PurchaseTransactionDialog.PurchaseTransactionResult result)
  {
    _ = RefreshTransactionsAsync();
    return Task.CompletedTask;
  }

  private Task HandlePurchaseCancelled() => Task.CompletedTask;

  private async Task RefreshTransactionsAsync()
  {
    if (InitialEnvelopeIdForNew > 0)
    {
      try
      {
        var list = await Api.GetTransactionsByEnvelopeAsync(InitialEnvelopeIdForNew);
        TransactionData = list.ToList();
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Refresh transactions failed: {ex.Message}");
      }
    }
  }
}
