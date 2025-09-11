using System.Diagnostics;
using System.Text.Json;
using Budget.DB;
using Budget.Web.Components.Envelopes;
using Microsoft.JSInterop;

namespace Budget.Web.Services;

internal sealed class EnvelopeState(IJSRuntime js)
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
  private const string StorageKey = "EnvelopeState_v1";

  internal List<EnvelopePage.EnvelopeResult>? AllEnvelopeData { get; private set; }
  internal List<EnvelopePage.Cat> Cats { get; private set; } = [];
  internal int? SelectedCategoryId { get; set; } = 0;

  internal bool IsLoaded => AllEnvelopeData != null;

  // Step 1: Load fast from localStorage if present; fall back to DB, then persist
  internal async Task EnsureLoadedAsync(BudgetContext db)
  {
    if (IsLoaded)
      return;

    // Try load from localStorage
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
          return; // Defer fresh data to RefreshFromDbAsync
        }
      }
    }
    catch
    {
      // ignore storage errors and fall back to DB
    }

    await RefreshFromDbAsync(db);
  }

  // Step 2: Refresh from DB (can be called in background) and persist to localStorage
  internal async Task RefreshFromDbAsync(BudgetContext db)
  {
    var data = from env in db.Envelopes
               join cat in db.Categories on env.CategoryId equals cat.Id
               orderby cat.SortOrder, env.SortOrder
               select new EnvelopePage.EnvelopeResult
               {
                 CategoryId = cat.Id,
                 CategoryName = cat.Name,
                 EnvelopeId = env.Id,
                 EnvelopeName = env.Name,
                 Balance = env.Balance,
                 Budget = env.Budget
               };

    Cats = [
      new EnvelopePage.Cat { CategoryId = 0, CategoryName = "All" },
      .. db.Categories
        .OrderBy(a => a.SortOrder)
        .Select(a => new EnvelopePage.Cat { CategoryId = a.Id, CategoryName = a.Name })
    ];

    AllEnvelopeData = [.. data];

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
