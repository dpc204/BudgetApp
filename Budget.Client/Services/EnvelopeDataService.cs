namespace Budget.Client.Services;

/// <summary>
/// Service for loading and transforming envelope data
/// </summary>
public class EnvelopeDataService(EnvelopeState state, IUserAndOptions userOptions) : IEnvelopeDataService
{
  /// <summary>
  /// Loads envelope data from cache or refreshes from API
  /// </summary>
  /// <param name="forceRefresh">If true, bypasses cache and loads from API</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result containing loaded envelope data</returns>
  public async Task<EnvelopeDataResult> LoadEnvelopeDataAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
  {
    if (forceRefresh || !state.IsLoaded)
    {
      await state.RefreshAsync();
    }
    else
    {
      await state.TryLoadFromCacheAsync();
      if (!state.IsLoaded)
      {
        await state.RefreshAsync();
      }
    }

    var categories = GetCategoriesForSelect();
    
    return new EnvelopeDataResult
    {
      AllEnvelopes = state.AllEnvelopeData ?? [],
      Categories = categories,
      SelectedCategoryId = userOptions.Options.SelectedCategoryType
    };
  }

  /// <summary>
  /// Gets categories available for selection based on user permissions
  /// </summary>
  /// <returns>List of categories the user can select</returns>
  public List<Cat> GetCategoriesForSelect()
  {
    return state.Cats;
  }

  /// <summary>
  /// Applies category filtering to envelope list
  /// </summary>
  /// <param name="allEnvelopes">All available envelopes</param>
  /// <param name="categories">Available categories</param>
  /// <param name="selectedCategoryId">Selected category ID (null or "0" for all)</param>
  /// <returns>Filtered list of envelopes</returns>
  public List<EnvelopeResult> ApplyCategoryFilter(
    List<EnvelopeResult> allEnvelopes,
    List<Cat> categories,
    string? selectedCategoryId)
  {
    if (string.IsNullOrEmpty(selectedCategoryId) || selectedCategoryId == "0")
    {
      // Return all envelopes that belong to the available categories
      return [.. allEnvelopes.Join(categories, e => e.CategoryId, c => c.CategoryId, (e, c) => e)];
    }

    // Filter by selected category
    return [.. allEnvelopes.Where(a => a.CategoryId == selectedCategoryId).OrderBy(a => a.EnvelopeId)];
  }

  /// <summary>
  /// Updates envelope balances with new values from API
  /// </summary>
  /// <param name="envelopes">List of envelopes with updated balances</param>
  public void UpdateEnvelopeBalances(List<EnvelopeDto> envelopes)
  {
    foreach (var env in envelopes)
    {
      var rec = state.AllEnvelopeData?.Find(e => e.EnvelopeId == env.Id);
      if (rec != null)
      {
        rec.Balance = env.Balance;
      }
    }
  }

  /// <summary>
  /// Saves the current state
  /// </summary>
  /// <param name="cancellationToken">Cancellation token</param>
  public async Task SaveStateAsync(CancellationToken cancellationToken = default)
  {
    await state.SaveAsync();
  }

  /// <summary>
  /// Refreshes envelope data from API
  /// </summary>
  /// <param name="cancellationToken">Cancellation token</param>
  public async Task RefreshAsync(CancellationToken cancellationToken = default)
  {
    await state.RefreshAsync();
  }
}

/// <summary>
/// Interface for envelope data operations
/// </summary>
public interface IEnvelopeDataService
{
  /// <summary>
  /// Loads envelope data from cache or refreshes from API
  /// </summary>
  Task<EnvelopeDataResult> LoadEnvelopeDataAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

  /// <summary>
  /// Gets categories available for selection based on user permissions
  /// </summary>
  List<Cat> GetCategoriesForSelect();

  /// <summary>
  /// Applies category filtering to envelope list
  /// </summary>
  List<EnvelopeResult> ApplyCategoryFilter(List<EnvelopeResult> allEnvelopes, List<Cat> categories, string? selectedCategoryId);

  /// <summary>
  /// Updates envelope balances with new values from API
  /// </summary>
  void UpdateEnvelopeBalances(List<EnvelopeDto> envelopes);

  /// <summary>
  /// Saves the current state
  /// </summary>
  Task SaveStateAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Refreshes envelope data from API
  /// </summary>
  Task RefreshAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of loading envelope data
/// </summary>
public class EnvelopeDataResult
{
  public List<EnvelopeResult> AllEnvelopes { get; set; } = [];
  public List<Cat> Categories { get; set; } = [];
  public string? SelectedCategoryId { get; set; }
}
