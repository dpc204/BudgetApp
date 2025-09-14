// Ensure you have the following NuGet package installed in your project:
// Microsoft.JSInterop

using Microsoft.JSInterop;
using System.Text.Json;
using Budget.Shared.Models;
using Budget.DTO;

namespace Budget.Shared.Services;

// Client-side version using API client + localStorage
public sealed class EnvelopeState(IJSRuntime js, IBudgetApiClient api)
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
  private const string StorageKey = "EnvelopeState_v1";
  private readonly IBudgetApiClient _api = api;

  public List<EnvelopeResult>? AllEnvelopeData { get; private set; }
  public List<Cat> Cats { get; private set; } = [];
  public int? SelectedCategoryId { get; set; } = 0;

  public bool IsLoaded => AllEnvelopeData != null;

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
            return; // defer to refresh later
        }
      }
    }
    catch { }

    await RefreshAsync();
  }

  public async Task RefreshAsync()
  {
    try
    {
      var categories = await _api.GetCategoriesAsync();
      var envelopes = await _api.GetEnvelopesAsync();

      Cats = [ new Cat { CategoryId = 0, CategoryName = "All" } ];
      Cats.AddRange(categories.Select(c => new Cat { CategoryId = c.Id, CategoryName = c.Name }));

      var categoryNameLookup = categories.ToDictionary(c => c.Id, c => c.Name);

      AllEnvelopeData = envelopes
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
    catch { }
  }

  private sealed class StateSnapshot
  {
    public List<EnvelopeResult>? AllEnvelopeData { get; set; }
    public List<Cat>? Cats { get; set; }
    public int? SelectedCategoryId { get; set; }
  }
}
