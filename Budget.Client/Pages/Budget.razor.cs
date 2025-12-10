using Budget.Client.Components.Dialogs;
using Budget.Shared.Utilities;

namespace Budget.Client.Pages;

public partial class Budget : ComponentBase
{
  [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
  private int MonthsToShow => _isSmallScreen ? 1 : 6;

  private const int SmallScreenBreakpoint = 768; // Bootstrap's md breakpoint
  private bool _isSmallScreen = false;

  private bool _loading = true;
  private Dictionary<int, Dictionary<DateTime, BudgetMonthData>>? _budgetData;
  private readonly List<BudgetDisplayRow> _displayRows = [];
  private readonly List<BudgetDisplayRow> _summaryRows = [];
  private readonly List<BudgetDisplayRow> _envelopeRows = [];
  private List<DateTime> _displayMonths = [];
  private int _currentScrollPosition = 0;

  protected override async Task OnInitializedAsync()
  {
    await LoadBudgetData();
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      // Note: Screen size is only checked on initial render for simplicity.
      // To support runtime resizing, add a JavaScript resize event listener.
      var previousValue = _isSmallScreen;
      await CheckScreenSize();
      if (previousValue != _isSmallScreen)
      {
        StateHasChanged();
      }
    }
  }

  private async Task CheckScreenSize()
  {
    try
    {
      var width = await JSRuntime.InvokeAsync<int>("windowUtils.getInnerWidth");
      _isSmallScreen = width < SmallScreenBreakpoint;
    }
    catch (JSException)
    {
      // Default to false if JS interop fails
      _isSmallScreen = false;
    }
    catch (JSDisconnectedException)
    {
      // Default to false if JS is disconnected
      _isSmallScreen = false;
    }
  }

  private async Task LoadBudgetData()
  {
    _loading = true;

    try
    {
      // Generate 12 months starting from current month (buffer for scrolling)
      var currentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
      _displayMonths = [.. Enumerable.Range(0, 12).Select(i => currentDate.AddMonths(i))];

      // Check if there are any draft values
      var hasDraftsResponse = await BudgetMonthlyApi.CheckDraftBudgetsAsync();

      if (hasDraftsResponse.HasDrafts)
      {
        var parameters = new DialogParameters
        {
          ["Message"] =
            $"You have {hasDraftsResponse.DraftCount} unsaved draft budget values. Do you want to continue with these drafts or reset them?"
        };

        var options = new DialogOptions { CloseOnEscapeKey = true };
        var dialog = await DialogService.ShowAsync<DraftConfirmationDialog>("Draft Budgets Found", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is bool keepDrafts && !keepDrafts)
        {
          await ClearDrafts();
        }
      }

      // Load all months of data
      _budgetData = [];

      foreach (var month in _displayMonths)
      {
        var monthData = await BudgetMonthlyApi.GetBudgetMonthAsync(month.Year, month.Month);

        foreach (var item in monthData)
        {
          if (!_budgetData.TryGetValue(item.EnvelopeId, out Dictionary<DateTime, BudgetMonthData>? value))
          {
            value = [];
            _budgetData[item.EnvelopeId] = value;
          }

          value[month] = new BudgetMonthData
          {
            EnvelopeId = item.EnvelopeId,
            EnvelopeName = item.EnvelopeName,
            CategoryId = item.CategoryId,
            CategoryName = item.CategoryName,
            CategoryType = item.CategoryType,
            SortOrder = item.SortOrder,
            BudgetValue = item.Budget,
            DraftValue = item.BudgetDraft,
            Month = month
          };
        }
      }

      BuildDisplayRows();
    }
    finally
    {
      _loading = false;
    }
  }

  private void BuildDisplayRows()
  {
    _displayRows.Clear();
    _summaryRows.Clear();
    _envelopeRows.Clear();

    if (_budgetData == null || _budgetData.Count == 0)
      return;

    // Get a sample month to extract envelope metadata
    var sampleMonth = _displayMonths.First();
    var envelopes = _budgetData.Values
      .Select(monthDict => monthDict.TryGetValue(sampleMonth, out BudgetMonthData? value) ? value : null)
      .Where(data => data != null)
      .OrderBy(data => data!.SortOrder)
      .ToList();

    // Separate by category type
    var incomeEnvelopes = envelopes.Where(e => e!.CategoryType == CatTypes.Income).ToList();
    var expenseEnvelopes = envelopes.Where(e => e!.CategoryType == CatTypes.User).ToList();

    // Add Net Budget row to summary
    var netBudgetRow = CreateSummaryRow("Net Budget", (month) =>
    {
      var (budget, draft) = CalculateTotals(incomeEnvelopes, month);
      var expenseTotals = CalculateTotals(expenseEnvelopes, month);
      return (budget - expenseTotals.budget, draft - expenseTotals.draft);
    });
    _summaryRows.Add(netBudgetRow);
    _displayRows.Add(netBudgetRow);

    // Add Total Income to summary
    var totalIncomeRow = CreateSummaryRow("Total Income", (month) => CalculateTotals(incomeEnvelopes, month));
    _summaryRows.Add(totalIncomeRow);
    _displayRows.Add(totalIncomeRow);

    // Add Total Expenses to summary
    var totalExpensesRow = CreateSummaryRow("Total Expenses", (month) => CalculateTotals(expenseEnvelopes, month));
    _summaryRows.Add(totalExpensesRow);
    _displayRows.Add(totalExpensesRow);

    // Add income envelopes to scrollable list
    foreach (var envelope in incomeEnvelopes)
    {
      var row = CreateEnvelopeRow(envelope!);
      _envelopeRows.Add(row);
      _displayRows.Add(row);
    }

    // Add expense envelopes to scrollable list
    foreach (var envelope in expenseEnvelopes)
    {
      var row = CreateEnvelopeRow(envelope!);
      _envelopeRows.Add(row);
      _displayRows.Add(row);
    }
  }

  private BudgetDisplayRow CreateEnvelopeRow(BudgetMonthData envelope)
  {
    var row = new BudgetDisplayRow
    {
      EnvelopeId = envelope.EnvelopeId,
      EnvelopeName = envelope.EnvelopeName,
      IsSummaryRow = false,
      MonthlyData = []
    };

    foreach (var month in _displayMonths)
    {
      if (_budgetData!.TryGetValue(envelope.EnvelopeId, out Dictionary<DateTime, BudgetMonthData>? value) &&
          value.TryGetValue(month, out BudgetMonthData? data))
      {
        row.MonthlyData[month] = new MonthCellData
        {
          DraftValue = data.DraftValue,
          BudgetValue = data.BudgetValue,
          DraftDisplayValue = data.DraftValue?.ToString("C2") ?? string.Empty
        };
      }
    }

    return row;
  }

  private BudgetDisplayRow CreateSummaryRow(string name,
    Func<DateTime, (decimal budget, decimal draft)> calculateTotals)
  {
    var row = new BudgetDisplayRow
    {
      EnvelopeId = 0,
      EnvelopeName = name,
      IsSummaryRow = true,
      MonthlyData = []
    };

    foreach (var month in _displayMonths)
    {
      var (budget, draft) = calculateTotals(month);
      row.MonthlyData[month] = new MonthCellData
      {
        DraftValue = null,
        BudgetValue = budget,
        DraftDisplayValue = draft.ToString("C2")
      };
    }

    return row;
  }

  private (decimal budget, decimal draft) CalculateTotals(List<BudgetMonthData?> envelopes, DateTime month)
  {
    decimal budgetTotal = 0;
    decimal draftTotal = 0;

    foreach (var envelope in envelopes.Where(e => e != null))
    {
      if (_budgetData!.ContainsKey(envelope!.EnvelopeId) &&
          _budgetData[envelope.EnvelopeId].TryGetValue(month, out BudgetMonthData? data))
      {
        budgetTotal += data.BudgetValue ?? 0;
        // Only include actual draft values, don't fall back to budget
        draftTotal += data.DraftValue ?? 0;
      }
    }

    return (budgetTotal, draftTotal);
  }

  private async Task UpdateDraft(int envelopeId, DateTime month, decimal? draftValue)
  {
    try
    {
      var acctPeriod = AcctPeriodHelper.DateToAcctPeriod(month);
      var response = await BudgetMonthlyApi.UpdateBudgetDraftAsync(acctPeriod, envelopeId, draftValue);

      if (response.Success)
      {
        // Update local data
        if (_budgetData!.TryGetValue(envelopeId, out Dictionary<DateTime, BudgetMonthData>? value) && value.TryGetValue(month, out BudgetMonthData? value1))
        {
          value1.DraftValue = draftValue;
        }

        BuildDisplayRows();
        StateHasChanged();
      }
    }
    catch (Exception ex)
    {
      Snackbar.Add($"Error updating draft: {ex.Message}", Severity.Error);
    }
  }

  private void ScrollLeft()
  {
    if (_currentScrollPosition > 0)
    {
      _currentScrollPosition--;
      StateHasChanged();
    }
  }

  private void ScrollRight()
  {
    // Allow scrolling but keep at least MonthsToShow months visible
    _currentScrollPosition++;

    // Load more months if needed
    var lastVisibleIndex = _currentScrollPosition + MonthsToShow - 1;
    while (lastVisibleIndex >= _displayMonths.Count)
    {
      var lastMonth = _displayMonths.Last();
      var newMonth = lastMonth.AddMonths(1);
      _displayMonths.Add(newMonth);
      // Load data for new month asynchronously
      _ = LoadMonthDataAsync(newMonth);
    }

    StateHasChanged();
  }

  private async Task LoadMonthDataAsync(DateTime month)
  {
    try
    {
      var monthData = await BudgetMonthlyApi.GetBudgetMonthAsync(month.Year, month.Month);

      if (_budgetData != null)
      {
        foreach (var item in monthData)
        {
          if (!_budgetData.TryGetValue(item.EnvelopeId, out Dictionary<DateTime, BudgetMonthData>? value))
          {
            value = [];
            _budgetData[item.EnvelopeId] = value;
          }

          value[month] = new BudgetMonthData
          {
            EnvelopeId = item.EnvelopeId,
            EnvelopeName = item.EnvelopeName,
            CategoryId = item.CategoryId,
            CategoryName = item.CategoryName,
            CategoryType = item.CategoryType,
            SortOrder = item.SortOrder,
            BudgetValue = item.Budget,
            DraftValue = item.BudgetDraft,
            Month = month
          };
        }

        BuildDisplayRows();
        await InvokeAsync(StateHasChanged);
      }
    }
    catch (Exception ex)
    {
      Snackbar.Add($"Error loading month data: {ex.Message}", Severity.Error);
    }
  }

  private async Task ClearDrafts()
  {
    var parameters = new DialogParameters
    {
      ["Message"] = "Are you sure you want to clear all draft budgets? This action cannot be undone."
    };

    var options = new DialogOptions { CloseOnEscapeKey = true };
    var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm Clear Drafts", parameters, options);
    var result = await dialog.Result;

    if (result != null && !result.Canceled && result.Data is bool confirmed && confirmed)
    {
      try
      {
        var response = await BudgetMonthlyApi.ClearDraftBudgetsAsync();

        if (response.Success)
        {
          Snackbar.Add("Draft budgets cleared successfully", Severity.Success);
          await LoadBudgetData();
        }
      }
      catch (Exception ex)
      {
        Snackbar.Add($"Error clearing drafts: {ex.Message}", Severity.Error);
      }
    }
  }

  private async Task ApplyDrafts()
  {
    var parameters = new DialogParameters
    {
      ["Message"] = "Are you sure you want to apply all draft budgets? This will update all budget values."
    };

    var options = new DialogOptions { CloseOnEscapeKey = true };
    var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm Apply Budgets", parameters, options);
    var result = await dialog.Result;

    if (result != null && !result.Canceled && result.Data is bool confirmed && confirmed)
    {
      try
      {
        var response = await BudgetMonthlyApi.ApplyDraftBudgetsAsync();

        if (response.Success)
        {
          Snackbar.Add("Draft budgets applied successfully", Severity.Success);
          await LoadBudgetData();
        }
      }
      catch (Exception ex)
      {
        Snackbar.Add($"Error applying drafts: {ex.Message}", Severity.Error);
      }
    }
  }

  private async Task CopyToNextMonth(int monthIndex, bool copyFromDraft)
  {
    try
    {
      // Bounds check
      if (monthIndex < 0 || monthIndex >= _displayMonths.Count)
      {
        Snackbar.Add("Invalid month index", Severity.Error);
        return;
      }

      var sourceMonth = _displayMonths[monthIndex];
      var sourceAcctPeriod = AcctPeriodHelper.DateToAcctPeriod(sourceMonth);

      // First attempt the copy (API will check for existing drafts)
      var response = await BudgetMonthlyApi.CopyBudgetToNextMonthAsync(sourceAcctPeriod, copyFromDraft);

      // If there's data to overwrite, show confirmation
      if (!response.Success && response.WouldOverwriteData)
      {
        var parameters = new DialogParameters
        {
          ["Message"] =
            "This action will overwrite data in the next month. Press Continue if this is what you want to do, otherwise press cancel.",
          ["ConfirmButtonText"] = "Continue"
        };

        var options = new DialogOptions { CloseOnEscapeKey = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm Overwrite", parameters, options);
        var dialogResult = await dialog.Result;

        if (dialogResult == null || dialogResult.Canceled ||
            (dialogResult.Data is bool continueAction && !continueAction))
        {
          return;
        }

        // User confirmed, perform the copy with confirmation flag
        response = await BudgetMonthlyApi.CopyBudgetToNextMonthAsync(sourceAcctPeriod, copyFromDraft,
          confirmOverwrite: true);
      }

      if (!response.Success)
      {
        Snackbar.Add($"Error: {response.Message}", Severity.Error);
        return;
      }

      Snackbar.Add(response.Message, Severity.Success);
      await LoadBudgetData();
    }
    catch (Exception ex)
    {
      Snackbar.Add($"Error copying to next month: {ex.Message}", Severity.Error);
    }
  }

  private void ToggleLock(int envelopeId, DateTime month)
  {
    // Find the row
    var row = _displayRows.FirstOrDefault(r => r.EnvelopeId == envelopeId);
    if (row != null && row.MonthlyData.TryGetValue(month, out MonthCellData? cellData))
    {
      cellData.IsLocked = !cellData.IsLocked;
      StateHasChanged();
    }
  }

  // Data models
  private class BudgetDisplayRow
  {
    public int EnvelopeId { get; set; }
    public string EnvelopeName { get; set; } = string.Empty;
    public bool IsSummaryRow { get; set; }
    public Dictionary<DateTime, MonthCellData> MonthlyData { get; set; } = [];
  }

  private class MonthCellData
  {
    public decimal? DraftValue { get; set; }
    public decimal? BudgetValue { get; set; }
    public string DraftDisplayValue { get; set; } = string.Empty;
    public bool IsLocked { get; set; } = false;
  }

  private class BudgetMonthData
  {
    public int EnvelopeId { get; set; }
    public string EnvelopeName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public CatTypes CategoryType { get; set; }
    public int SortOrder { get; set; }
    public decimal? BudgetValue { get; set; }
    public decimal? DraftValue { get; set; }
    public DateTime Month { get; set; }
  }
}