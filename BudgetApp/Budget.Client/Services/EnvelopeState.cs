using System.Text.Json;
using Microsoft.JSInterop;
using Budget.Client.Components.Envelopes;

namespace Budget.Client.Services;

// Client-side version: replaces DbContext with HttpClient/localStorage. Keep TODOs where BudgetContext was used.
internal sealed class EnvelopeState(IJSRuntime js)
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
  private const string StorageKey = "EnvelopeState_v1";

  internal List<EnvelopePage.EnvelopeResult>? AllEnvelopeData { get; private set; }
  internal List<EnvelopePage.Cat> Cats { get; private set; } = [];
  internal int? SelectedCategoryId { get; set; } = 0;

  internal bool IsLoaded => AllEnvelopeData != null;

  // TODO: Previously EnsureLoadedAsync(BudgetContext db)
  internal async Task EnsureLoadedAsync()
  {
    if (IsLoaded)
      return;

    try
    {
      var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
      if (!string.IsNullOrWhiteSpace(json))
      {
        var snapshot = JsonSerializer.Deserialize<StateSnapshot>(json, _jsonOptions);
        if (snapshot is not null && snapshot.AllEnvelopeData is not null)
        {
          AllEnvelopeData = snapshot.AllEnvelopeData;
          Cats = snapshot.Cats ?? [];
          SelectedCategoryId = snapshot.SelectedCategoryId;
          return; // Defer fresh data to RefreshAsync
        }
      }
    }
    catch
    {
      // ignore storage errors
    }

    await RefreshAsync();
  }

  // TODO: Previously RefreshFromDbAsync(BudgetContext db)
  internal async Task RefreshAsync()
  {
    // TODO: Replace with API call to Budget.Api for envelopes + categories
    // For now, keep empty lists so page renders.
    Cats = Cats.Count == 0 ? [ new EnvelopePage.Cat { CategoryId = 0, CategoryName = "All" } ] : Cats;
    AllEnvelopeData ??= [];

    await SaveAsync();
  }

  internal async Task SaveAsync()
  {
    try
    {
      var snapshot = new StateSnapshot
      {
        AllEnvelopeData = AllEnvelopeData,
        Cats = Cats,
        SelectedCategoryId = SelectedCategoryId,
      };
      var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
      await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }
    catch
    {
      // ignore storage errors
    }
  }

  private sealed class StateSnapshot
  {
    public List<EnvelopePage.EnvelopeResult>? AllEnvelopeData { get; set; }
    public List<EnvelopePage.Cat>? Cats { get; set; }
    public int? SelectedCategoryId { get; set; }
  }
}
