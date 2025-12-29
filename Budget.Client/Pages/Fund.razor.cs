using Azure;
using Budget.Client.Components.Dialogs;
using Budget.Shared.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Budget.Client.Pages;

public partial class Fund : ComponentBase
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
    }
  }

  /// <summary>
  /// Loads fund data for the currently selected month and prepares the component's display rows.
  /// </summary>
  /// <remarks>
  /// Sets the internal loading flag while fetching month data, populates the internal fund data map and aggregate totals, computes the available-to-fund placeholder value, and rebuilds the UI display rows. Ensures the loading flag is cleared when complete.
  /// </remarks>
  private async Task LoadFundDataAsync()
  {
    _loading = true;

    try
    {
      var monthData = await BudgetMonthlyApi.GetBudgetMonthAsync(_selectedMonth.Year, _selectedMonth.Month);

      var allocateEnvelope =await BudgetMonthlyApi.GetEnvelopeByEnvelopeTypeAsync(EnvelopeTypes.Unallocated);

      //if(!allocateEnvelope.IsCompletedSuccessfully)
      //{
      //  await InvokeAsync(() =>
      //  { 
      //    Snackbar.Add(allocateEnvelope.Exception?.Message ?? "Unable to get Unallocated Envelope", Severity.Warning);
      //  });
      //  return;
      //}

      _availableToFund = allocateEnvelope.Balance;

      _fundData = [];
      _totalBudget = 0;
      _totalBalance = 0;

      foreach (var item in monthData.Where(a=> a.CategoryType == CatTypes.User))
      {
        var envelopeData = new FundEnvelopeData
        {
          EnvelopeId = item.EnvelopeId,
          EnvelopeName = item.EnvelopeName,
          CategoryId = item.CategoryId,
          CategoryName = item.CategoryName,
          CategoryType = item.CategoryType,
          SortOrder = item.SortOrder,
          Budget = item.Budget,
          CurrentBalance = item.Balance, // Placeholder: In production, this would come from Envelope.Balance
          FundAmount = item.FundAmount
        };
        _availableToFund -= envelopeData.FundAmount ?? 0m;
        _fundData[item.EnvelopeId] = envelopeData;

        // Calculate totals
        _totalBudget += item.Budget ?? 0;
        // Placeholder: In production, balance would come from Envelope table
        _totalBalance = 850.00m;
      }


      BuildDisplayRows();
    }
    finally
    {
      _loading = false;
    }
  }

  /// <summary>
  /// Builds the list of display rows used by the UI from the current internal fund data, ordered by each envelope's SortOrder.
  /// </summary>
  /// <remarks>
  /// Clears any existing rows and repopulates <see cref="_envelopeRows"/> from <see cref="_fundData"/> entries, copying EnvelopeId, EnvelopeName, CurrentBalance, Budget, and FundAmount for each envelope.
  /// If <see cref="_fundData"/> is null or empty, the method leaves <see cref="_envelopeRows"/> empty.
  /// </remarks>
  private void BuildDisplayRows()
  {
    _envelopeRows.Clear();

    if (_fundData == null || _fundData.Count == 0)
      return;

    // Sort envelopes by SortOrder
    var sortedEnvelopes = _fundData.Values
      .OrderBy(e => e.SortOrder)
      .ToList();

    foreach (var envelope in sortedEnvelopes)
    {
      _envelopeRows.Add(new FundDisplayRow
      {
        EnvelopeId = envelope.EnvelopeId,
        EnvelopeName = envelope.EnvelopeName,
        CurrentBalance = envelope.CurrentBalance,
        Budget = envelope.Budget,
        FundAmount = envelope.FundAmount
      });
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
  /// For each envelope with a Budget, sets its FundAmount to the greater of 0 and (Budget * selected percentage) minus CurrentBalance.
  /// After updating envelopes, rebuilds display rows, requests a UI refresh, and shows a success snackbar indicating the applied fill.
  /// </remarks>
  private async Task AllocateFill()
  {
    if (_fundData == null) return;

    foreach (var envelope in _fundData.Values)
    {
      if (envelope.Budget.HasValue)
      {
        await AllocateOneEnvelope(envelope);
      }
    }

    BuildDisplayRows();
    StateHasChanged();

    Snackbar.Add($"Applied {GetFillButtonText()} to all envelopes", Severity.Success);
  }

  private async Task AllocateOneEnvelope(int envelopeId, FillAmounts oneHundredPercent)
  {
    var envelope = _fundData?[envelopeId];
    if (envelope != null)
      await AllocateOneEnvelope(envelope);
  }

  private async Task AllocateOneEnvelope(FundEnvelopeData envelope, FillAmounts fillType = FillAmounts.NotSet)
  {
    if (!envelope.Budget.HasValue)
      return;


    if (fillType == FillAmounts.NotSet)
      fillType = _selectedFillType;

    var budgetAmount = envelope.Budget.Value;

    var targetAmount = 0.0m;

    switch (fillType)
    {
      case FillAmounts.OneHundredPercent:
        targetAmount = budgetAmount;
        break;
      case FillAmounts.FiftyPercent:
        // You may want to implement logic here for 50% fill
        targetAmount = budgetAmount * .5m;
        break;
      case FillAmounts.FillToBudget:
        if (envelope.CurrentBalance >= budgetAmount)
          targetAmount = budgetAmount - envelope.CurrentBalance;
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }

    envelope.FundAmount = targetAmount;
    await UpdateFundAmountAsync(envelope.EnvelopeId, targetAmount);
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
        var response = await BudgetMonthlyApi.UpdateFundAmountAsync(envelopeId, fundAmount);

        if (response.Success)
        {
          // Update local data
          envelope.FundAmount = fundAmount;
          if (fundAmount != null) _availableToFund -= fundAmount.Value;

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
    }
  }

  /// <summary>
  /// Set an envelope's FundAmount to the amount required to reach its budget for the selected period.
  /// </summary>
  /// <param name="envelopeId">The identifier of the envelope to update.</param>
  /// <remarks>
  /// If the envelope has no budget defined, no changes are made. When updated, the method rebuilds the display rows, triggers a UI refresh, and shows a success notification.
  /// </remarks>
  private async void FillToBudgetForPeriod(int envelopeId)
  {
    if (_fundData != null && _fundData.TryGetValue(envelopeId, out FundEnvelopeData? envelope))
    {
      if (envelope.Budget.HasValue)
      {
       await AllocateOneEnvelope(envelope);
      }
    }
  }

  /// <summary>
  /// Set the fund amount for the specified envelope to its full budget for the selected period.
  /// </summary>
  /// <param name="envelopeId">The identifier of the envelope to update.</param>
  private void AddFullBudgetAmountForPeriod(int envelopeId)
  {
    if (_fundData != null && _fundData.TryGetValue(envelopeId, out FundEnvelopeData? envelope))
    {
      if (envelope.Budget.HasValue)
      {
        // Add full budget amount regardless of current balance
        envelope.FundAmount = envelope.Budget.Value;
        BuildDisplayRows();
        StateHasChanged();

        Snackbar.Add($"Set {envelope.EnvelopeName} to full budget amount", Severity.Success);
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
        var response = await BudgetMonthlyApi.ClearAllFundAmountsAsync();

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
        _processing = false;
        StateHasChanged();
      }
    }
  }

 
  // Data models
  private class FundEnvelopeData
  {
    public int EnvelopeId { get; set; }
    public string EnvelopeName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public CatTypes CategoryType { get; set; }
    public int SortOrder { get; set; }
    public decimal? Budget { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal? FundAmount { get; set; }
  }

  private class FundDisplayRow
  {
    public int EnvelopeId { get; set; }
    public string EnvelopeName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal? Budget { get; set; }
    public decimal? FundAmount { get; set; }
    public int UpdateCounter { get; set; }
  }
}