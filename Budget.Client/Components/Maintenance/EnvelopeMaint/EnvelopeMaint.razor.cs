using FailureEventArgs = Syncfusion.Blazor.Grids.FailureEventArgs;

namespace Budget.Client.Components.Maintenance.EnvelopeMaint;

public partial class EnvelopeMaint : ComponentBase
{
  private SfGrid<EnvelopeDto>? GridRef;

  protected override void OnInitialized()
  {
    // initialization logic if needed
  }

  protected override void OnAfterRender(bool firstRender)
  {
    if (firstRender)
    {
      CategoryParams = new DropDownEditCellParams
      {
        Params = new()
        {
          DataSource = new List<CategoryDto>()
          {
            new() { Id = 1, Name = "Frequent", Description = "", SortOrder = 0 },
            new() { Id = 2, Name = "Regular", Description = "", SortOrder = 1 },
            new() { Id = 3, Name = "Bills", Description = "", SortOrder = 2 },
          },
          PopupWidth = "100%"
        }
      };
    }
  }

  public IEditorSettings? CategoryParams;

  public int Count { get; set; }

  public static void DataBoundHandler()
  {  Console.WriteLine("DataBoundHandler ");
    throw new NotImplementedException();
  
  }

  public static void ActionBeginHandler(ActionEventArgs<EnvelopeDto> args)
  {
    Console.WriteLine("Begin Handler");
  }

  public static void ActionFailureHandler(FailureEventArgs args)
  {
    Console.WriteLine("FailureHandler1");
  }

  public static void ActionCompletedHandler(ActionEventArgs<EnvelopeDto> args)
  {
    Console.WriteLine("CompletedHandler");
  }
}