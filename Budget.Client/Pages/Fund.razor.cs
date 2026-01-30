using Budget.Client.Components.Dialogs;
using Budget.Client.Services;
using Budget.Shared.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Budget.Client.Pages;

public partial class Fund(IFundDataService fundDataService, IFundAllocationService allocationService) : ComponentBase
{
  private bool _loading = true;
  private bool _processing = false;
  private Dictionary<int, FundEnvelopeData>? _fundData;
  private readonly List<FundDisplayRow> _envelopeRows = [];
  private List<DateTime> _monthOptions = [];
  private DateTime _selectedMonth;
  private FillAmounts _selectedFillType = FillAmounts.OneHundredPercent;

  private decimal _totalBudget = 0;
  private decimal _totalBalance = 0;
  private decimal _availableToFund = 0;
  private decimal _totalToFund;
  private decimal _originalAvailableToFund;
  public bool HideFundButton => _selectedFillType == FillAmounts.NotSet;
  public bool NotReadyToFill => (_availableToFund < 0 || _totalToFund == 0);

  /// <summary>
  /// Initialize the selectable month options (previous, current, next), select the current month, and load fund data for that month.
  /// </summary>
  /// <returns>A task that completes when initialization and fund data loading have finished.</returns>
  protected override async Task OnInitializedAsync()
  {
    // Initialize month options: prior month, current month, next month
    var currentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    _monthOptions =
    [
      new DateTime(2025, 10, 1),
      new DateTime(2025, 11, 1),
      currentDate.AddMonths(-1),
      currentDate,
      currentDate.AddMonths(1)
    ];

    _selectedMonth = currentDate;

    await LoadFundDataAsync();
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      // Initialize fund field auto-select on focus
      await JsRuntime.InvokeVoidAsync("initializeFundFieldAutoSelect");
      _selectedFillType = UserAndOptions.Options.FillAmountType;
    }
  }

  /// <summary>
  /// Loads fund data for the currently selected month and prepares the component's display rows.
  /// </summary>
  /// <remarks>
  /// Uses FundDataService to load and transform data, then updates component state with results.
  /// </remarks>
  private async Task LoadFundDataAsync()
  {
    _loading = true;

    try
    {
      var result = await fundDataService.LoadFundDataAsync(_selectedMonth.Year, _selectedMonth.Month);

      _fundData = result.FundData;
      _totalBudget = result.TotalBudget;
      _totalBalance = result.TotalBalance;
      _availableToFund = result.AvailableToFund;
      _originalAvailableToFund = _availableToFund;
      _envelopeRows.Clear();
      _envelopeRows.AddRange(fundDataService.BuildDisplayRows(_fundData));
    }
    finally
    {
      _loading = false;
    }
  }

  /// <summary>
  /// Updates the component's selected month and reloads fund data for that month.
  /// </summary>
  /// <param name="newMonth">The newly selected month to display and load data for.</param>
  private async Task OnMonthChanged(DateTime newMonth)
  {
    _selectedMonth = newMonth;
    await LoadFundDataAsync();
  }

  /// <summary>
  /// Selects the fill-amount preset to use for funding operations and refreshes the component UI.
  /// </summary>
  /// <param name="fillAmount">The preset fill amount to apply (e.g., OneHundredPercent or FiftyPercent).</param>
  private void SetFillAmount(FillAmounts fillAmount)
  {
    UserAndOptions.Options.FillAmountType = fillAmount;
    _selectedFillType = fillAmount;
    StateHasChanged();
  }

  /// <summary>
  /// Returns the label text for the fill button based on the currently selected fill amount.
  /// </summary>
  /// <returns>The button label from the Display attribute of the selected fill amount.</returns>
  private string GetFillButtonText()
  {
    return GetDisplayName(_selectedFillType);
  }

  /// <summary>
  /// Gets the display name from the Display attribute of an enum value.
  /// </summary>
  /// <param name="fillAmount">The enum value to get the display name for.</param>
  /// <returns>The display name from the Display attribute, or the enum value name if no attribute is found.</returns>
  private static string GetDisplayName(FillAmounts fillAmount)
  {
    var memberInfo = typeof(FillAmounts).GetMember(fillAmount.ToString()).FirstOrDefault();
    var displayAttribute = memberInfo?.GetCustomAttribute<DisplayAttribute>();
    return displayAttribute?.Name ?? fillAmount.ToString();
  }

  /// <summary>
  /// Applies the currently selected fill percentage to each envelope's target funding amount for the selected month.
  /// </summary>
  /// <remarks>
  /// Uses FundAllocationService to calculate amounts, then updates each envelope and persists changes.
  /// </remarks>
  private async Task AllocateFill()
  {
    if (_fundData == null) return;

    var envelopesWithBudget = _fundData.Values.Where(e => e.Budget.HasValue).ToList();
    var calculations = allocationService.CalculateFundAmounts(envelopesWithBudget, _selectedFillType);

    foreach (var (envelopeId, amount) in calculations)
    {
      await UpdateFundAmountAsync(envelopeId, amount);
    }

    StateHasChanged();
    Snackbar.Add($"Applied {GetFillButtonText()} to all envelopes", Severity.Success);
  }

  private async Task AllocateOneEnvelope(int envelopeId, FillAmounts fillType)
  {
    if (_fundData == null || !_fundData.TryGetValue(envelopeId, out var envelope))
      return;

    if (!envelope.Budget.HasValue)
      return;

    var amount = allocationService.CalculateFundAmount(envelope.Budget, envelope.CurrentBalance, fillType);
    await UpdateFundAmountAsync(envelopeId, amount);
  }

  /// <summary>
  /// Sets the pending fund amount for the specified envelope, persists the change to the backend, and refreshes the UI display.
  /// </summary>
  /// <param name="envelopeId">Identifier of the envelope to update.</param>
  /// <param name="fundAmount">New fund amount to assign, or null to clear the pending amount.</param>
  private async Task UpdateFundAmountAsync(int envelopeId, decimal? fundAmount)
  {
    if (_fundData != null && _fundData.TryGetValue(envelopeId, out FundEnvelopeData? envelope))
    {
      try
      {
        _availableToFund += envelope.FundAmount ?? 0; // Reclaim previous amount
        var response = await api.UpdateFundAmountAsync(envelopeId, fundAmount);

        if (response.Success)
        {
          // Update local data
          envelope.FundAmount = fundAmount;
          if (fundAmount != null)
          {
            _availableToFund -= fundAmount.Value;
          }

          // Update the display row data without rebuilding entire table (prevents focus stealing)
          var row = _envelopeRows.FirstOrDefault(r => r.EnvelopeId == envelopeId);
          if (row != null)
          {
            row.FundAmount = fundAmount;
            row.UpdateCounter++; // Force MudNumericField recreation for proper formatting
          }

          await InvokeAsync(StateHasChanged);
        }
        else
        {
          // Validation error - show message
          await InvokeAsync(() => { Snackbar.Add(response.Message ?? "Validation error", Severity.Warning); });
        }
      }
      catch (Exception ex)
      {
        Snackbar.Add($"Error updating fund amount: {ex.Message}", Severity.Error);
      }
      finally
      {
        _totalToFund = 0;
        _availableToFund = _originalAvailableToFund;
        foreach (var env in _envelopeRows)
        {
          _totalToFund += env.FundAmount?? 0;
          _availableToFund -= env.FundAmount ?? 0;
        }
      }
    }
  }

  /// <summary>
  /// Display help information about using the Fund screen.
  /// </summary>
  /// <remarks>
  /// Shows an informational snackbar explaining the Fill button (automatic funding based on budget percentages)
  /// and the three-dot menu for filling individual envelopes.
  /// </remarks>
  private void ShowHelp()
  {
    Snackbar.Add(
      "Fund screen help: Use the Fill button to automatically calculate funding amounts based on budget percentages. Use the three-dot menu to fill individual envelopes.",
      Severity.Info);
  }

  private async Task CalculateFund()
  {
  }


  /// <summary>
  /// Clears all fund amounts across all envelopes, returning the fund dollars to available funds, and persists the changes to the backend.
  /// </summary>
  /// <remarks>
  /// Prompts the user for confirmation before clearing. If confirmed, clears all local fund amounts, recalculates available funds, 
  /// calls the API to persist changes, and reloads the page on API failure to restore prior stored values.
  /// </remarks>
  private async Task ClearFundAmounts()
  {
    var parameters = new DialogParameters
    {
      ["Message"] = "Are you sure you want to clear all fund amounts? This action will reset all fund values to zero."
    };

    var options = new DialogOptions { CloseOnEscapeKey = true };
    var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm Clear Fund Amounts", parameters, options);
    var result = await dialog.Result;

    if (result is { Canceled: false, Data: true })
    {
      try
      {
        _processing = true;
        StateHasChanged();

        // Calculate total to return to available funds
        decimal totalToReturn = 0m;
        if (_fundData != null)
        {
          foreach (var envelope in _fundData.Values)
          {
            totalToReturn += envelope.FundAmount ?? 0m;
            envelope.FundAmount = 0m;
          }

          // Return fund dollars to available funds
          _availableToFund += totalToReturn;
        }

        // Update display rows
        foreach (var row in _envelopeRows)
        {
          row.FundAmount = 0m;
          row.UpdateCounter++;
        }

        StateHasChanged();

        // Call API to persist changes
        var response = await api.ClearAllFundAmountsAsync();

        if (response.Success)
        {
          Snackbar.Add($"Cleared {response.RecordsUpdated} fund amounts successfully", Severity.Success);
        }
        else
        {
          Snackbar.Add($"Error: {response.Message}", Severity.Error);
          // Reload page to restore prior values
          await LoadFundDataAsync();
        }
      }
      catch (Exception ex)
      {
        Snackbar.Add($"Error clearing fund amounts: {ex.Message}", Severity.Error);
        // Reload page to restore prior values
        await LoadFundDataAsync();
      }
      finally
      {
        _totalToFund = 0;
        _processing = false;
        StateHasChanged();
      }
    }
  }

  private async Task FundEnvelopes(MouseEventArgs arg)
  {
    await Task.CompletedTask;
  }
}