// Ensure you have the following NuGet package installed in your project:
// Microsoft.JSInterop

namespace Budget.Shared.Services;

// Client-side version using API client + localStorage (deferred until after first interactive render)
public class EnvelopeState(IJSRuntime js, IBudgetApiClient api, ILogger<EnvelopeState> logger)
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
  private const string StorageKey = "EnvelopeState_v1";
  private readonly IBudgetApiClient _api = api;
  private readonly ILogger<EnvelopeState> _logger = logger;


  /// <summary>
  /// Indicates that the owning component is currently executing OnInitializedAsync, so
  /// JavaScript interop calls (such as localStorage load/save) should be suppressed.
  /// </summary>


  public bool InOnInitializedAsync { get; set; }
  public virtual List<EnvelopeResult>? AllEnvelopeData { get; set; }
  public virtual List<Cat> Cats { get; set; } = [];
  public virtual string? SelectedCategoryId { get; set; } = "0";

  public virtual bool IsLoaded => AllEnvelopeData != null;
  private bool _cacheAttempted; // ensures we only try localStorage once after interactive render

  // Call as early as possible (OnInitializedAsync) � performs ONLY server/API work (no JS)
  public async Task EnsureLoadedAsync()
  {
    if(IsLoaded)
      return;
    await RefreshAsync(); // API fetch only; caching happens later
  }

  // Invoke from a component's OnAfterRenderAsync(firstRender) to hydrate from localStorage once JS is available.
  public virtual async Task TryLoadFromCacheAsync()
  {
    if(_cacheAttempted)
      return;
    _cacheAttempted = true;

    try
    {
      if(InOnInitializedAsync)
        return;


      var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
      if(!string.IsNullOrWhiteSpace(json))
      {
        var snapshot = JsonSerializer.Deserialize<StateSnapshot>(json, _jsonOptions);
        if(snapshot is not null && snapshot.AllEnvelopeData is not null)
        {
          AllEnvelopeData = snapshot.AllEnvelopeData;
          Cats = snapshot.Cats ?? Cats;
          SelectedCategoryId = snapshot.SelectedCategoryId;
          return; // do not immediately re-save; SaveAsync guarded
        }
      }
    }
    catch(Exception ex)
    {
      // Swallow � may still be prerendering or JS not yet ready; we'll rely on RefreshAsync data.
      _logger.LogDebug(ex, "Skipping cache load (JS not ready or failed).");
    }
  }

  public virtual async Task RefreshAsync()
  {
    try
    {
      var categories = await _api.GetCategoriesAsync();
      var envelopes = await _api.GetEnvelopesAsync();
      _cacheAttempted = false;
      Cats = [new Cat { CategoryId = "0", CategoryName = "All" }];
      Cats.AddRange(categories.Select(c => new Cat { CategoryId = c.CategoryId, SortOrder = c.SortOrder, CategoryName = c.Name, CatType = c.CatType }));

      var categoryNameLookup = categories.ToDictionary(c => c.CategoryId, c => c);

      AllEnvelopeData = [.. envelopes
        .Select(e => new EnvelopeResult
        {
          CategoryId = e.CategoryId,
          CategoryName = categoryNameLookup.TryGetValue(e.CategoryId, out var catName) ? catName.Name : string.Empty,
          CategorySortOrder = categoryNameLookup.TryGetValue(e.CategoryId, out var catOrder) ? catOrder.SortOrder : 0,
          EnvelopeId = e.Id,
          EnvelopeName = e.Name,
          EnvelopeSortOrder = e.SortOrder,
          Balance = e.Balance,
          Budget = e.Budget,
          EnvelopeType = e.EnvelopeType
        })
        .OrderBy(e => e.CategoryId)
        .ThenBy(e => e.EnvelopeName)];

      _ = SaveAsync();

    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Failed refreshing envelope data from API");
      Cats = Cats.Count == 0 ? [new Cat { CategoryId = "0", CategoryName = "All" }] : Cats;
      AllEnvelopeData ??= [];
    }


  }

  public virtual async Task SaveAsync()
  {
    // Only persist to localStorage after we've attempted cache load (i.e., interactive render occurred)
    //if (!_cacheAttempted)
    //  return;

    try
    {
      if(InOnInitializedAsync)
        return;

      var snapshot = new StateSnapshot {
        AllEnvelopeData = AllEnvelopeData,
        Cats = Cats,
        SelectedCategoryId = SelectedCategoryId,
      };
      var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
      await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }
    catch(Exception ex) when((ex is InvalidOperationException && ex.Message.Contains("JavaScript interop calls cannot be issued at this time"))
    || ex is JSException)
    {
      // Ignore � typically occurs if called just before JS is fully ready; non-fatal.
      _logger.LogDebug(ex, "Skipping localStorage save (JS unavailable).");
    }
    catch(Exception ex)
    {
      _logger.LogWarning(ex, "Unexpected error saving EnvelopeState to localStorage key {StorageKey}", StorageKey);
    }
  }

  private sealed class StateSnapshot
  {
    public List<EnvelopeResult>? AllEnvelopeData { get; set; }
    public List<Cat>? Cats { get; set; }
    public string? SelectedCategoryId { get; set; }
  }
}
