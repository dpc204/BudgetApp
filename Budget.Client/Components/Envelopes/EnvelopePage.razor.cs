using Budget.Shared.Models;
using Budget.Shared.Services;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Grids;
using Budget.Client.Components.Transactions;

namespace Budget.Client.Components.Envelopes;

public partial class EnvelopePage : ComponentBase
{
  [Inject] private EnvelopeState State { get; set; } = default!;
  [Inject] private IBudgetApiClient Api { get; set; } = default!;

  public List<EnvelopeResult>? AllEnvelopeData => State.AllEnvelopeData;
  public List<EnvelopeResult>? SelectedEnvelopeData { get; set; } = [];
  public List<TransactionDto>? TransactionData { get; set; } = [];

  private bool ShowTransactionDialog { get; set; }
  private TransactionDto? SelectedTransaction { get; set; }
  private OneTransactionDetail? SelectedTransactionDetail { get; set; }

  private bool ShowPurchaseDialog { get; set; }
  private int InitialEnvelopeIdForNew { get; set; }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if(firstRender)
    {
      await State.EnsureLoadedAsync();
      ApplySelection();
      StateHasChanged();

      await State.RefreshAsync();
      ApplySelection();
      StateHasChanged();
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
    StateHasChanged();

    SelectedTransactionDetail = await Api.GetOneTransactionDetailAsync(args.RowData.TransactionId);
    StateHasChanged();
  }

  private async Task RowSelectHandler(RowSelectEventArgs<EnvelopeResult> args)
  {
    if (args?.Data is null)
      return;

    var rslt = await Api.GetTransactionsByEnvelopeAsync(args.Data.EnvelopeId);
    TransactionData = rslt.ToList();
    InitialEnvelopeIdForNew = args.Data.EnvelopeId; // track for new transaction default
    StateHasChanged();
  }

  private void OnShowTransactionDialogChanged(bool value)
  {
    ShowTransactionDialog = value;
    StateHasChanged();
  }

  private void NewTransaction(int envelopeId)
  {
    InitialEnvelopeIdForNew = envelopeId;
    ShowPurchaseDialog = true;
    StateHasChanged();
  }

  private void OnPurchaseDialogVisibleChanged(bool value)
  {
    ShowPurchaseDialog = value;
    StateHasChanged();
  }

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
      var list = await Api.GetTransactionsByEnvelopeAsync(InitialEnvelopeIdForNew);
      TransactionData = list.ToList();
      StateHasChanged();
    }
  }
}
