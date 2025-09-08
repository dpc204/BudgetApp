using Budget.DB;
using Budget.Web.Components.Envelopes;

namespace Budget.Web.Services;

internal sealed class EnvelopeState
{
  internal List<EnvelopePage.EnvelopeResult>? AllEnvelopeData { get; private set; }
  internal List<EnvelopePage.Cat> Cats { get; private set; } = new();
  internal int? SelectedCategoryId { get; set; } = 0;

  internal bool IsLoaded => AllEnvelopeData != null;

  internal void EnsureLoaded(BudgetContext db)
  {
    if (IsLoaded) return;

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

    Cats = db.Categories
      .OrderBy(a => a.SortOrder)
      .Select(a => new EnvelopePage.Cat { CategoryId = a.Id, CategoryName = a.Name })
      .ToList();

    Cats.Insert(0, new EnvelopePage.Cat { CategoryId = 0, CategoryName = "All" });

    AllEnvelopeData = data.ToList();
  }
}
