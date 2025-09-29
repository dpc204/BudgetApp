using Budget.DTO;
using Syncfusion.Blazor.Grids;

namespace Budget.Client.Components.Maintenance.EnvelopeMaint;

public partial class EnvelopeMaint
{
  private SfGrid<EnvelopeDto>? GridRef;

  public int count { get; set; }

  public void DataBoundHandler(BeforeDataBoundArgs<EnvelopeDto> args)
  {
    // Optional: logic after data bound.
  }

  public void ActionBeginHandler(ActionEventArgs<EnvelopeDto> args)
  {
    // Handle beginning of grid actions (add/edit/delete)
  }

  public void ActionFailureHandler(FailureEventArgs args)
  {
    // Log or display failure details
  }

  public void ActionCompletedHandler(ActionEventArgs<EnvelopeDto> args)
  {
    // If needed, force refresh after save:
    // if (args.RequestType == Action.Save || args.RequestType == "save") _ = GridRef?.Refresh();
  }
}