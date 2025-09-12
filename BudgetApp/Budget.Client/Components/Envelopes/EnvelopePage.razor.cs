using Microsoft.AspNetCore.Components;
using Budget.Shared.Models;
using Budget.Shared.Services;


namespace Budget.Client.Components.Envelopes;

public partial class EnvelopePage(EnvelopeState State) : ComponentBase
{


  public List<EnvelopeResult>? AllEnvelopeData => State.AllEnvelopeData;
  public List<EnvelopeResult>? SelectedEnvelopeData { get; set; }

  protected override void OnInitialized()
  {
    // Initial render shows nothing; we load in OnAfterRenderAsync to allow JS interop
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      // 1) Fast path: load from localStorage and render
      await State.EnsureLoadedAsync(); // TODO: previously EnsureLoadedAsync(BudgetContext db)
      ApplySelection();
      StateHasChanged();

      // 2) Then refresh in background from API and re-render when done
      await State.RefreshAsync(); // TODO: previously RefreshFromDbAsync(BudgetContext db)
      ApplySelection();
      StateHasChanged();
    }
  }

  private void ApplySelection()
  {
    var selected = SelectedCategoryId ?? 0;
    SelectedEnvelopeData = selected == 0
      ? AllEnvelopeData
      : AllEnvelopeData?.Where(a => a.CategoryId == selected).ToList();
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
}
