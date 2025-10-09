using Budget.Shared.Models;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.TreeGrid.Internal;

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
        Params = new DropDownListModel<object, object>()
        {
          DataSource = new List<CategoryDto>()
          {
            new CategoryDto { Id = 1, Name = "Frequent", Description = "", SortOrder = 0 },
            new CategoryDto { Id = 2, Name = "Regular", Description = "", SortOrder = 1 },
            new CategoryDto { Id = 3, Name = "Bills", Description = "", SortOrder = 2 },
          },
          PopupWidth = "100%"
        }
      };
    }
  }

  public IEditorSettings? CategoryParams;

  public int count { get; set; }

  public void DataBoundHandler(BeforeDataBoundArgs<EnvelopeDto> args)
  {
   Console.WriteLine("DataBoundHandler ");
  }

  public void ActionBeginHandler(ActionEventArgs<EnvelopeDto> args)
  {
    Console.WriteLine("Begin Handler");
  }

  public void ActionFailureHandler(FailureEventArgs args)
  {
    Console.WriteLine("FailureHandler1");
  }

  public void ActionCompletedHandler(ActionEventArgs<EnvelopeDto> args)
  {
    Console.WriteLine("CompletedHandler");
  }
}