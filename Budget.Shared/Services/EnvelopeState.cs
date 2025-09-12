// Ensure you have the following NuGet package installed in your project:
// Microsoft.JSInterop

using Microsoft.JSInterop;
using System.Text.Json;
using Budget.Shared.Models;

namespace Budget.Shared.Services;

// Client-side version: replaces DbContext with HttpClient/localStorage. Keep TODOs where BudgetContext was used.
public sealed class EnvelopeState(IJSRuntime js)
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
  private const string StorageKey = "EnvelopeState_v1";

  public List<EnvelopeResult>? AllEnvelopeData { get; private set; }
  public List<Cat> Cats { get; private set; } = [];
  public int? SelectedCategoryId { get; set; } = 0;

  public bool IsLoaded => AllEnvelopeData != null;

  // TODO: Previously EnsureLoadedAsync(BudgetContext db)
  public async Task EnsureLoadedAsync()
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
  public async Task RefreshAsync()
  {
    // TODO: Replace with API call to Budget.Api for envelopes + categories
    // For now, keep empty lists so page renders.
    Cats = Cats.Count == 0 ? [ new Cat { CategoryId = 0, CategoryName = "All" } ] : Cats;
    AllEnvelopeData ??= [];

    await SaveAsync();
  }

  public async Task SaveAsync()
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
    public List<EnvelopeResult>? AllEnvelopeData { get; set; }
    public List<Cat>? Cats { get; set; }
    public int? SelectedCategoryId { get; set; }
  }
}
