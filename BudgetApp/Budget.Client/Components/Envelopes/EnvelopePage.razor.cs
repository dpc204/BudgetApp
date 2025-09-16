using Budget.DTO;
using Budget.Shared.Models;
using Budget.Shared.Services;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Grids;

namespace Budget.Client.Components.Envelopes;

public partial class EnvelopePage(EnvelopeState State, IBudgetApiClient api) : ComponentBase
{
  public List<EnvelopeResult>? AllEnvelopeData => State.AllEnvelopeData;
  public List<EnvelopeResult>? SelectedEnvelopeData { get; set; } = [];
  public List<TransactionDto>? TransactionData { get; set; } = [];

  private bool ShowTransactionDialog { get; set; }
  private TransactionDto? SelectedTransaction { get; set; }

  protected override void OnInitialized()
  {
    // Initial render shows nothing; we load in OnAfterRenderAsync to allow JS interop
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
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
    Console.WriteLine($"Value: {value}");
  }

  public void RecordDoubleClickHandler(RecordDoubleClickEventArgs<TransactionDto> args)
  {
    if (args?.RowData is null)
      return;

    SelectedTransaction = args.RowData;
    ShowTransactionDialog = true;
    StateHasChanged();
  }

  private async Task RowSelectHandler(RowSelectEventArgs<EnvelopeResult> args)
  {
    if (args?.Data is null)
      return;

    var rslt = await api.GetTransactionsByEnvelopeAsync(args.Data.EnvelopeId);
    TransactionData = rslt.ToList();
    StateHasChanged();
  }

  private void CloseTransactionDialog(Microsoft.AspNetCore.Components.Web.MouseEventArgs _)
  {
    ShowTransactionDialog = false;
  }
}
