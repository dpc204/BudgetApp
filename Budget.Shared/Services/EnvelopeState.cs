// Ensure you have the following NuGet package installed in your project:
// Microsoft.JSInterop

using Microsoft.JSInterop;
using System.Text.Json;
using Budget.Shared.Models;
using System.Net.Http;
using System.Net.Http.Json;

namespace Budget.Shared.Services;

// Client-side version: replaces DbContext with HttpClient/localStorage. Keep TODOs where BudgetContext was used.
public sealed class EnvelopeState(IJSRuntime js, HttpClient http)
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
  private const string StorageKey = "EnvelopeState_v1";
  private readonly HttpClient _http = http;

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
    try
    {
      // Fetch categories and envelopes from API
      var categories = await _http.GetFromJsonAsync<List<CategoryDto>>("categories/getbyenvelopeid");
      var envelopes = await _http.GetFromJsonAsync<List<EnvelopeDto>>("envelopes/getall");

      // Build category list with synthetic "All" category
      Cats = [ new Cat { CategoryId = 0, CategoryName = "All" } ];
      if (categories is not null)
      {
        Cats.AddRange(categories.Select(c => new Cat { CategoryId = c.Id, CategoryName = c.Name }));
      }

      var categoryNameLookup = (categories ?? []).ToDictionary(c => c.Id, c => c.Name);

      AllEnvelopeData = (envelopes ?? [])
        .Select(e => new EnvelopeResult
        {
          CategoryId = e.CategoryId,
          CategoryName = categoryNameLookup.TryGetValue(e.CategoryId, out var catName) ? catName : string.Empty,
          EnvelopeId = e.Id,
          EnvelopeName = e.Name,
          Balance = e.Balance,
          Budget = e.Budget
        })
        .OrderBy(e => e.CategoryId)
        .ThenBy(e => e.EnvelopeName)
        .ToList();
    }
    catch
    {
      // On failure keep existing data but ensure non-null collections
      Cats = Cats.Count == 0 ? [ new Cat { CategoryId = 0, CategoryName = "All" } ] : Cats;
      AllEnvelopeData ??= [];
    }

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

  // Local DTOs matching API responses (avoid direct dependency on client service layer)
  private sealed record EnvelopeDto(int Id, string Name, decimal Balance, decimal Budget, int CategoryId, int SortOrder);
  private sealed record CategoryDto(int Id, string Name, string Description, int SortOrder);
}
