using Budget.Shared.Utilities;

namespace Budget.Client.Pages;

public partial class Fund : ComponentBase
{
  private bool _loading = true;
  private bool _processing = false;
  private Dictionary<int, FundEnvelopeData>? _fundData;
  private readonly List<FundDisplayRow> _envelopeRows = [];
  private List<DateTime> _monthOptions = [];
  private DateTime _selectedMonth;
  private FillAmounts _selectedFillAmount = FillAmounts.OneHundredPercent;

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

      _fundData = [];
      _totalBudget = 0;
      _totalBalance = 0;

      foreach (var item in monthData)
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
          CurrentBalance = 0, // Placeholder: In production, this would come from Envelope.Balance
          FundAmount = null
        };

        _fundData[item.EnvelopeId] = envelopeData;

        // Calculate totals
        _totalBudget += item.Budget ?? 0;
        // Placeholder: In production, balance would come from Envelope table
        _totalBalance = 850.00m;
      }

      // Placeholder: In production, this would be calculated from actual account balances
      _availableToFund = 1300.00m;

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
    _selectedFillAmount = fillAmount;
    StateHasChanged();
  }

  /// <summary>
  /// Returns the label text for the fill button based on the currently selected fill amount.
  /// </summary>
  /// <returns>The button label: "Fill 100%", "Fill 50%", or "Fill".</returns>
  private string GetFillButtonText()
  {
    return _selectedFillAmount switch
    {
      FillAmounts.OneHundredPercent => "Fill 100%",
      FillAmounts.FiftyPercent => "Fill 50%",
      _ => "Fill"
    };
  }

  /// <summary>
  /// Applies the currently selected fill percentage to each envelope's target funding amount for the selected month.
  /// </summary>
  /// <remarks>
  /// For each envelope with a Budget, sets its FundAmount to the greater of 0 and (Budget * selected percentage) minus CurrentBalance.
  /// After updating envelopes, rebuilds display rows, requests a UI refresh, and shows a success snackbar indicating the applied fill.
  /// </remarks>
  private void ApplyFillAmounts()
  {
    if (_fundData == null) return;

    foreach (var envelope in _fundData.Values)
    {
      if (envelope.Budget.HasValue)
      {
        var budgetAmount = envelope.Budget.Value;
        var fillPercentage = _selectedFillAmount == FillAmounts.OneHundredPercent ? 1.0m : 0.5m;
        
        // Calculate fund amount as percentage of budget minus current balance
        var targetAmount = budgetAmount * fillPercentage;
        envelope.FundAmount = Math.Max(0, targetAmount - envelope.CurrentBalance);
      }
    }

    BuildDisplayRows();
    StateHasChanged();
    
    Snackbar.Add($"Applied {GetFillButtonText()} to all envelopes", Severity.Success);
  }

  /// <summary>
  /// Sets the pending fund amount for the specified envelope and refreshes the UI display.
  /// </summary>
  /// <param name="envelopeId">Identifier of the envelope to update.</param>
  /// <param name="fundAmount">New fund amount to assign, or null to clear the pending amount.</param>
  private void UpdateFundAmount(int envelopeId, decimal? fundAmount)
  {
    if (_fundData != null && _fundData.TryGetValue(envelopeId, out FundEnvelopeData? envelope))
    {
      envelope.FundAmount = fundAmount;
      BuildDisplayRows();
      StateHasChanged();
    }
  }

  /// <summary>
  /// Set an envelope's FundAmount to the amount required to reach its budget for the selected period.
  /// </summary>
  /// <param name="envelopeId">The identifier of the envelope to update.</param>
  /// <remarks>
  /// If the envelope has no budget defined, no changes are made. When updated, the method rebuilds the display rows, triggers a UI refresh, and shows a success notification.
  /// </remarks>
  private void FillToBudgetForPeriod(int envelopeId)
  {
    if (_fundData != null && _fundData.TryGetValue(envelopeId, out FundEnvelopeData? envelope))
    {
      if (envelope.Budget.HasValue)
      {
        // Fill to budget means: budget amount minus current balance
        envelope.FundAmount = Math.Max(0, envelope.Budget.Value - envelope.CurrentBalance);
        BuildDisplayRows();
        StateHasChanged();
        
        Snackbar.Add($"Set {envelope.EnvelopeName} to fill to budget", Severity.Success);
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
    Snackbar.Add("Fund screen help: Use the Fill button to automatically calculate funding amounts based on budget percentages. Use the three-dot menu to fill individual envelopes.", Severity.Info);
  }

  // Enum for fill amounts
  public enum FillAmounts
  {
    OneHundredPercent,
    FiftyPercent
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
  }
}