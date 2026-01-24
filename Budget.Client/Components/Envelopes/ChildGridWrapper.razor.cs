using Budget.Shared.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Budget.Client.Components.Envelopes;

/// <summary>
/// Wrapper component for child transaction grids that allows parent to track and refresh them
/// </summary>
public partial class ChildGridWrapper : ComponentBase
{
  [Parameter, EditorRequired] public int EnvelopeId { get; set; }
  [Parameter, EditorRequired] public EnvelopeResult EnvelopeItem { get; set; } = default!;
  [Parameter, EditorRequired] public Action<int, MudDataGrid<EnvelopeTransactionListItem>?> OnGridCreated { get; set; } = default!;
  [Parameter, EditorRequired] public Func<EnvelopeResult?, GridStateVirtualize<EnvelopeTransactionListItem>, CancellationToken, Task<GridData<EnvelopeTransactionListItem>>> ServerDataFunc { get; set; } = default!;
  [Parameter, EditorRequired] public Func<EnvelopeTransactionListItem, bool, Task> OnTransactionRowClick { get; set; } = default!;

  private MudDataGrid<EnvelopeTransactionListItem>? _grid;

  protected override void OnAfterRender(bool firstRender)
  {
    if (firstRender && _grid != null)
    {
      OnGridCreated(EnvelopeId, _grid);
    }
  }
}
