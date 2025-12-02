using Budget.Client.Components.Dialogs;
using Budget.Shared.Enums;
using Budget.Shared.Utilities;

namespace Budget.Client.Pages;

public partial class Budget : ComponentBase
{
  private const int MonthsToShow = 6;
  
  private bool _loading = true;
  private Dictionary<int, Dictionary<DateTime, BudgetMonthData>>? _budgetData;
  private List<BudgetDisplayRow> _displayRows = new();
  private List<DateTime> _displayMonths = new();
  private int _currentScrollPosition = 0;

  protected override async Task OnInitializedAsync()
  {
    await LoadBudgetData();
  }

  private async Task LoadBudgetData()
  {
    _loading = true;
    
    try
    {
      // Generate 12 months starting from current month (buffer for scrolling)
      var currentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
      _displayMonths = Enumerable.Range(0, 12)
        .Select(i => currentDate.AddMonths(i))
        .ToList();

      // Check if there are any draft values
      var hasDraftsResponse = await BudgetMonthlyApi.CheckDraftBudgetsAsync();
      
      if (hasDraftsResponse.HasDrafts)
      {
        var parameters = new DialogParameters
        {
          ["Message"] = $"You have {hasDraftsResponse.DraftCount} unsaved draft budget values. Do you want to continue with these drafts or reset them?"
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
      _budgetData = new Dictionary<int, Dictionary<DateTime, BudgetMonthData>>();
      
      foreach (var month in _displayMonths)
      {
        var monthData = await BudgetMonthlyApi.GetBudgetMonthAsync(month.Year, month.Month);
        
        foreach (var item in monthData)
        {
          if (!_budgetData.ContainsKey(item.EnvelopeId))
          {
            _budgetData[item.EnvelopeId] = new Dictionary<DateTime, BudgetMonthData>();
          }
          
          _budgetData[item.EnvelopeId][month] = new BudgetMonthData
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

    if (_budgetData == null || !_budgetData.Any())
      return;

    // Get a sample month to extract envelope metadata
    var sampleMonth = _displayMonths.First();
    var envelopes = _budgetData.Values
      .Select(monthDict => monthDict.ContainsKey(sampleMonth) ? monthDict[sampleMonth] : null)
      .Where(data => data != null)
      .OrderBy(data => data!.SortOrder)
      .ToList();

    // Separate by category type
    var incomeEnvelopes = envelopes.Where(e => e!.CategoryType == CatTypes.Income).ToList();
    var expenseEnvelopes = envelopes.Where(e => e!.CategoryType == CatTypes.User).ToList();

    // Add Net Budget row
    _displayRows.Add(CreateSummaryRow("Net Budget", (month) =>
    {
      var incomeTotals = CalculateTotals(incomeEnvelopes, month);
      var expenseTotals = CalculateTotals(expenseEnvelopes, month);
      return (incomeTotals.budget - expenseTotals.budget, incomeTotals.draft - expenseTotals.draft);
    }));

    // Add Total Income section
    _displayRows.Add(CreateSummaryRow("Total Income", (month) => CalculateTotals(incomeEnvelopes, month)));
    
    foreach (var envelope in incomeEnvelopes)
    {
      _displayRows.Add(CreateEnvelopeRow(envelope!));
    }

    // Add Total Expenses section
    _displayRows.Add(CreateSummaryRow("Total Expenses", (month) => CalculateTotals(expenseEnvelopes, month)));
    
    foreach (var envelope in expenseEnvelopes)
    {
      _displayRows.Add(CreateEnvelopeRow(envelope!));
    }
  }

  private BudgetDisplayRow CreateEnvelopeRow(BudgetMonthData envelope)
  {
    var row = new BudgetDisplayRow
    {
      EnvelopeId = envelope.EnvelopeId,
      EnvelopeName = envelope.EnvelopeName,
      IsSummaryRow = false,
      MonthlyData = new Dictionary<DateTime, MonthCellData>()
    };

    foreach (var month in _displayMonths)
    {
      if (_budgetData!.ContainsKey(envelope.EnvelopeId) && 
          _budgetData[envelope.EnvelopeId].ContainsKey(month))
      {
        var data = _budgetData[envelope.EnvelopeId][month];
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

  private BudgetDisplayRow CreateSummaryRow(string name, Func<DateTime, (decimal budget, decimal draft)> calculateTotals)
  {
    var row = new BudgetDisplayRow
    {
      EnvelopeId = 0,
      EnvelopeName = name,
      IsSummaryRow = true,
      MonthlyData = new Dictionary<DateTime, MonthCellData>()
    };

    foreach (var month in _displayMonths)
    {
      var totals = calculateTotals(month);
      row.MonthlyData[month] = new MonthCellData
      {
        DraftValue = null,
        BudgetValue = totals.budget,
        DraftDisplayValue = totals.draft.ToString("C2")
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
          _budgetData[envelope.EnvelopeId].ContainsKey(month))
      {
        var data = _budgetData[envelope.EnvelopeId][month];
        budgetTotal += data.BudgetValue;
        draftTotal += data.DraftValue ?? data.BudgetValue;
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
        if (_budgetData!.ContainsKey(envelopeId) && _budgetData[envelopeId].ContainsKey(month))
        {
          _budgetData[envelopeId][month].DraftValue = draftValue;
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
          if (!_budgetData.ContainsKey(item.EnvelopeId))
          {
            _budgetData[item.EnvelopeId] = new Dictionary<DateTime, BudgetMonthData>();
          }
          
          _budgetData[item.EnvelopeId][month] = new BudgetMonthData
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
      var sourceMonth = _displayMonths[monthIndex];
      var targetMonth = sourceMonth.AddMonths(1);
      var sourceAcctPeriod = AcctPeriodHelper.DateToAcctPeriod(sourceMonth);

      // Check if target month has draft data
      var targetMonthData = await BudgetMonthlyApi.GetBudgetMonthAsync(targetMonth.Year, targetMonth.Month);
      var hasTargetDrafts = targetMonthData.Any(m => m.BudgetDraft.HasValue);

      // Show confirmation if there's data to overwrite
      if (hasTargetDrafts)
      {
        var parameters = new DialogParameters
        {
          ["Message"] = "This action will overwrite data in the next month. Press Continue if this is what you want to do, otherwise press cancel."
        };
        
        var options = new DialogOptions { CloseOnEscapeKey = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm Overwrite", parameters, options);
        var dialogResult = await dialog.Result;
        
        if (dialogResult == null || dialogResult.Canceled || (dialogResult.Data is bool continueAction && !continueAction))
        {
          return;
        }
      }

      // Perform the copy operation
      var response = await BudgetMonthlyApi.CopyBudgetToNextMonthAsync(sourceAcctPeriod, copyFromDraft);
      
      if (response.Success)
      {
        Snackbar.Add(response.Message, Severity.Success);
        await LoadBudgetData();
      }
    }
    catch (Exception ex)
    {
      Snackbar.Add($"Error copying to next month: {ex.Message}", Severity.Error);
    }
  }

  // Data models
  private class BudgetDisplayRow
  {
    public int EnvelopeId { get; set; }
    public string EnvelopeName { get; set; } = string.Empty;
    public bool IsSummaryRow { get; set; }
    public Dictionary<DateTime, MonthCellData> MonthlyData { get; set; } = new();
  }

  private class MonthCellData
  {
    public decimal? DraftValue { get; set; }
    public decimal BudgetValue { get; set; }
    public string DraftDisplayValue { get; set; } = string.Empty;
  }

  private class BudgetMonthData
  {
    public int EnvelopeId { get; set; }
    public string EnvelopeName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public CatTypes CategoryType { get; set; }
    public int SortOrder { get; set; }
    public decimal BudgetValue { get; set; }
    public decimal? DraftValue { get; set; }
    public DateTime Month { get; set; }
  }
}
