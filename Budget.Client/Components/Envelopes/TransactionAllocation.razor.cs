using Budget.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace Budget.Client.Components.Envelopes;

public partial class TransactionAllocation : ComponentBase
{
  [Inject] private EnvelopeState State { get; set; } = default!;
  [Inject] private IBudgetApiClient Api { get; set; } = default!;
  [Inject] private ILogger<EnvelopePage> Logger { get; set; } = default!;
  [Inject] private IUserAndOptions UserOptions { get; set; } = default!;

  public List<TransactionDto> Transactions { get; set; } = [];

  private EnvelopeResult? _selectedEnvelope;

 

  // Height calculation so the MudDataGrid shows exactly3 rows and scrolls for more
  // Dense row height in MudBlazor is ~33px; header is ~56px. Adjust if theme differs.
  private const int EnvelopeRowHeightPx = 38;
  private const int EnvelopeHeaderHeightPx = 56;
  private static string EnvelopeGridHeightPx => $"{(EnvelopeRowHeightPx * 5) + EnvelopeHeaderHeightPx}px";

  private const int TransactionRowHeightPx = 38;
  private const int TransactionHeaderHeightPx = 56;
  private static string TransactionGridHeightPx => $"{(TransactionRowHeightPx * 5) + TransactionHeaderHeightPx}px";


  private bool _loading = true;
  private string? _loadError;
  private bool _afterRenderInit;

  protected override async Task OnInitializedAsync()
  {
    try
    {
      // Ensure selection class applied on first render when an item is already selected
      await InvokeAsync(StateHasChanged);

      Transactions = await Api.GetTransactionsUnallocatedAsync();
    }
    catch (Exception ex)
    {
      _loadError = ex.Message;
      if (Logger.IsEnabled(LogLevel.Error))
      {
        Logger.LogError(ex, "Error in OnInitializedAsync");
      }
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    //if (firstRender && !_afterRenderInit)
    //{
    //  _afterRenderInit = true;
    //  var runtimeType = JSRuntime.GetType().Name;
    //  if (Logger.IsEnabled(LogLevel.Information))
    //  {
    //    Logger.LogInformation("EnvelopePage.OnAfterRenderAsync - Runtime: {Runtime}", runtimeType);
    //  }

    //  Console.WriteLine($"OnAfterRenderAsync running on: {runtimeType}");

    //  try
    //  {
    //    await State.TryLoadFromCacheAsync();
    //    if (!State.IsLoaded)
    //    {
    //      await State.RefreshAsync();
    //    }

    //    CategoriesForSelect = GetCategoriesForSelect();
    //    ApplyCategorySelection();
    //  }
    //  catch (Exception ex)
    //  {
    //    _loadError = ex.Message;
    //    Logger.LogError(ex, "Error in OnAfterRenderAsync");
    //  }
    //  finally
    //  {
    //    _loading = false;
    //    StateHasChanged();
    //  }
    //}
    _loading = false;
    StateHasChanged();
  }

  private void OnRowClicked(DataGridRowClickEventArgs<TransactionDto> args)
  {
    if (args?.Item is null) return;
  }

  private void OnEnvelopeRowClick(TableRowClickEventArgs<EnvelopeResult> args)
  {
    if (args?.Item is null) return;
//    SelectedEnvelope = args.Item;
  }

  private async Task OnSelectedEnvelopeChangedAsync(EnvelopeResult? envelope)
  {
   
  }

  //private string? GetEnvelopeRowClass(EnvelopeResult item, int rowNumber)
  //  => SelectedEnvelope?.EnvelopeId == item.EnvelopeId ? "row-selected-secondary" : null;

  //private string? GetEnvelopeRowStyle(EnvelopeResult item, int rowNumber)
  //  => SelectedEnvelope?.EnvelopeId == item.EnvelopeId
  //    ? "background-color: var(--mud-palette-gray-dark); color: var(--mud-palette-secondary-contrastText);"
  //    : null;
}