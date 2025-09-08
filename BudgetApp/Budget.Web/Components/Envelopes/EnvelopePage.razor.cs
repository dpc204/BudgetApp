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
    // Load from state (loads once per session/tab)
    State.EnsureLoaded(Db);
    SelectedEnvelopeData = AllEnvelopeData;
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


  private void CatChanged(ChangeEventArgs<int?, Cat> args)
  {
    var selected = args.Value ?? 0;
    SelectedCategoryId = selected;

    if (selected == 0)
      SelectedEnvelopeData = AllEnvelopeData;
    else
      SelectedEnvelopeData = AllEnvelopeData?.Where(a => a.CategoryId == selected).ToList();

    Console.WriteLine($"Value: {args.Value}");
  }
}