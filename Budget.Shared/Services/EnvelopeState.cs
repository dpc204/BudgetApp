// Ensure you have the following NuGet package installed in your project:
// Microsoft.JSInterop

using Microsoft.JSInterop;
using System.Text.Json;
using Budget.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Budget.Shared.Services;

// Client-side version using API client + localStorage (deferred until after first interactive render)
public sealed class EnvelopeState(IJSRuntime js, IBudgetApiClient api, ILogger<EnvelopeState> logger)
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
  private const string StorageKey = "EnvelopeState_v1";
  private readonly IBudgetApiClient _api = api;
  private readonly ILogger<EnvelopeState> _logger = logger;

  public List<EnvelopeResult>? AllEnvelopeData { get; private set; }
  public List<Cat> Cats { get; private set; } = [];
  public int? SelectedCategoryId { get; set; } = 0;

  public bool IsLoaded => AllEnvelopeData != null;
  private bool _cacheAttempted; // ensures we only try localStorage once after interactive render

  // Call as early as possible (OnInitializedAsync) – performs ONLY server/API work (no JS)
  public async Task EnsureLoadedAsync()
  {
    if (IsLoaded)
      return;
    await RefreshAsync(); // API fetch only; caching happens later
  }

  // Invoke from a component's OnAfterRenderAsync(firstRender) to hydrate from localStorage once JS is available.
  public async Task TryLoadFromCacheAsync()
  {
    if (_cacheAttempted)
      return;
    _cacheAttempted = true;

    try
    {
      var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
      if (!string.IsNullOrWhiteSpace(json))
      {
        var snapshot = JsonSerializer.Deserialize<StateSnapshot>(json, _jsonOptions);
        if (snapshot is not null && snapshot.AllEnvelopeData is not null)
        {
          AllEnvelopeData = snapshot.AllEnvelopeData;
          Cats = snapshot.Cats ?? Cats;
          SelectedCategoryId = snapshot.SelectedCategoryId;
          return; // do not immediately re-save; SaveAsync guarded
        }
      }
    }
    catch (Exception ex)
    {
      // Swallow – may still be prerendering or JS not yet ready; we'll rely on RefreshAsync data.
      _logger.LogDebug(ex, "Skipping cache load (JS not ready or failed).");
    }
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
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed refreshing envelope data from API");
      Cats = Cats.Count == 0 ? [ new Cat { CategoryId = 0, CategoryName = "All" } ] : Cats;
      AllEnvelopeData ??= [];
    }

    await SaveAsync();
  }

  public async Task SaveAsync()
  {
    // Only persist to localStorage after we've attempted cache load (i.e., interactive render occurred)
    if (!_cacheAttempted)
      return;

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
    catch (Exception ex) when (ex is InvalidOperationException or JSException)
    {
      // Ignore – typically occurs if called just before JS is fully ready; non-fatal.
      _logger.LogDebug(ex, "Skipping localStorage save (JS unavailable).");
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Unexpected error saving EnvelopeState to localStorage key {StorageKey}", StorageKey);
    }
  }

  private sealed class StateSnapshot
  {
    public List<EnvelopeResult>? AllEnvelopeData { get; set; }
    public List<Cat>? Cats { get; set; }
    public int? SelectedCategoryId { get; set; }
  }
}
