using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.DropDowns;
using Budget.Web.Services;

namespace Budget.Web.Components.Envelopes;

public partial class EnvelopePage
{
  [Inject] internal EnvelopeState State { get; set; } = default!;

  internal List<EnvelopeResult>? AllEnvelopeData => State.AllEnvelopeData;
  internal List<EnvelopeResult>? SelectedEnvelopeData { get; set; }

  protected override void OnInitialized()
  {
    // Initial render shows nothing; we load in OnAfterRenderAsync to allow JS interop
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      // 1) Fast path: load from localStorage and render
      await State.EnsureLoadedAsync(Db);
      ApplySelection();
      StateHasChanged();

      // 2) Then refresh in background from DB and re-render when done
      await State.RefreshFromDbAsync(Db);
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

  public class Cat
  {
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
  }

  public int? SelectedCategoryId
  {
    get => State.SelectedCategoryId;
    set => State.SelectedCategoryId = value;
  }

  public sealed record EnvelopeResult
  {
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int EnvelopeId { get; init; }
    public string EnvelopeName { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public decimal Budget { get; init; }
  }


  private async Task CatChanged(ChangeEventArgs<int?, Cat> args)
  {
    var selected = args.Value ?? 0;
    SelectedCategoryId = selected;

    ApplySelection();

    await State.SaveAsync();

    Console.WriteLine($"Value: {args.Value}");
  }
}