using Budget.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace Budget.Client.Components.Envelopes;

public partial class ShowOneTransaction
{
  [Parameter] public OneTransactionDetail? Transaction { get; set; }
  [Parameter] public bool Visible { get; set; }
  [Parameter] public EventCallback<bool> VisibleChanged { get; set; }

  private async Task Close(Microsoft.AspNetCore.Components.Web.MouseEventArgs _)
  {
    await SetVisible(false);
  }

  private async Task SetVisible(bool value)
  {
    if (Visible != value)
    {
      Visible = value;
      await VisibleChanged.InvokeAsync(value);
      StateHasChanged();
    }
  }
}