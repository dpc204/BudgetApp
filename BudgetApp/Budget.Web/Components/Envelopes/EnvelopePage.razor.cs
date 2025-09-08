using Syncfusion.Blazor.DropDowns;

namespace Budget.Web.Components.Envelopes;

public partial class EnvelopePage
{
  internal List<EnvelopeResult>? AllEnvelopeData { get; set; }
  internal List<EnvelopeResult>? SelectedEnvelopeData { get; set; }

  protected override void OnInitialized()
  {
    if (AllEnvelopeData == null)
    {
      var data = from env in Db.Envelopes
        join cat in Db.Categories on env.CategoryId equals cat.Id
        orderby cat.SortOrder, env.SortOrder
        select new EnvelopeResult
        {
          CategoryId = cat.Id, CategoryName = cat.Name, EnvelopeId = env.Id, EnvelopeName = env.Name,
          Balance = env.Balance, Budget = env.Budget
        };

      Cats = Db.Categories.OrderBy(a => a.SortOrder).Select(a => new Cat() { CategoryId = a.Id, CategoryName = a.Name })
        .ToList();
      Cats.Insert(0, new Cat() { CategoryId = 0, CategoryName = "All" });

      AllEnvelopeData = data.ToList();
    }

    SelectedEnvelopeData = AllEnvelopeData;
  }


  internal List<Cat> Cats { get; set; } = new();

  public class Cat
  {
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
  }

  public int? SelectedCategoryId { get; set; } = 0;

  internal sealed record EnvelopeResult
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
    var v = args.Value;

    if (args.Value == 0)
      SelectedEnvelopeData = AllEnvelopeData;
    else
      SelectedEnvelopeData = AllEnvelopeData.Where(a => a.CategoryId == SelectedCategoryId).ToList();


    Console.WriteLine($"Value: {args.Value}");
  }
}