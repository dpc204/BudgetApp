using Budget.Client.Components.Dialogs;
using Budget.Shared.Utilities;

namespace Budget.Client.Pages;

public partial class Budget : ComponentBase
{
  [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
  private int MonthsToShow { get; set; }

  private const int SmallScreenBreakpoint = 768; // Bootstrap's md breakpoint
  private bool _isSmallScreen;

  private bool _loading = true;
  private bool _processing;
  private Dictionary<int, Dictionary<DateTime, BudgetMonthData>>? _budgetData;
  private readonly List<BudgetDisplayRow> _displayRows = [];
  private readonly List<BudgetDisplayRow> _summaryRows = [];
  private readonly List<BudgetDisplayRow> _envelopeRows = [];
  private List<DateTime> _displayMonths = [];
  private int _currentScrollPosition;
  private int _MonthProgress = 0;
  private int _totalMonths = 0;
  private const int DefaultScreenColumns = 3;

  protected override async Task OnInitializedAsync()
  {
    MonthsToShow = _isSmallScreen ? 1 : DefaultScreenColumns;
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

      // Initialize draft field navigation
      await JsRuntime.InvokeVoidAsync("initializeDraftFieldNavigation");
    }
  }

  /// <summary>
  /// Sets the component's small-screen flag based on the browser's inner width.
  /// </summary>
  /// <remarks>
  /// Reads window inner width via JavaScript interop and sets <c>_isSmallScreen</c> to true when the width is less than <c>SmallScreenBreakpoint</c>. If JavaScript interop fails or is disconnected, <c>_isSmallScreen</c> is set to false.
  /// </remarks>
  private async Task CheckScreenSize()
  {
    try
    {
      var width = await JsRuntime.InvokeAsync<int>("windowUtils.getInnerWidth");
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

  /// <summary>
  /// Loads budget data for a 12-month window into the component's state, optionally prompting the user about existing draft values before loading.
  /// </summary>
  /// <param name="checkForDrafts">If true, checks for unsaved draft budget values and prompts the user to keep or reset them before loading; if false, skips the draft check.</param>
  private async Task LoadBudgetData(bool checkForDrafts = true)
  {
    _loading = true;

    try
    {
      // Generate 12 months starting from current month (buffer for scrolling)
      var currentDate = new DateTime(2025, 10, 1);
      //  var currentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
      _displayMonths = [.. Enumerable.Range(0, MonthsToShow).Select(i => currentDate.AddMonths(i))];

      // Check if there are any draft values
      if (checkForDrafts)
      {
        var hasDraftsResponse = await BudgetMonthlyApi.CheckDraftBudgetsAsync();

        if (hasDraftsResponse.HasDrafts)
        {
          var parameters = new DialogParameters
          {
            ["Message"] =
              $"You have {hasDraftsResponse.DraftCount} unsaved draft budget values. Do you want to continue with these drafts or reset them?"
          };

          var options = new DialogOptions { CloseOnEscapeKey = true };
          var dialog =
            await DialogService.ShowAsync<DraftConfirmationDialog>("Draft Budgets Found", parameters, options);
          var result = await dialog.Result;

          if (result is { Canceled: false, Data: false } dialogResult)
          {
            await ClearDrafts();
          }
        }
      }

      // Load all months of data
      _budgetData = [];
      _processing = true;
      _totalMonths = _displayMonths.Count;
      _MonthProgress = 0;
      foreach (var month in _displayMonths)
      {
        _MonthProgress++;
        await LoadMonthDataAsync(month);
      }

      BuildDisplayRows();
    }
    finally
    {
      _loading = false;
      _processing = false;
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
      .Select(monthDict => monthDict.GetValueOrDefault(sampleMonth))
      .Where(data => data != null)
      .OrderBy(data => data!.SortOrder)
      .ToList();

    // Separate by category type
    var incomeEnvelopes = envelopes.Where(e => e!.CategoryType == CatTypes.Income).ToList();
    var expenseEnvelopes = envelopes.Where(e => e!.CategoryType == CatTypes.User).ToList();


    // Add Total Income to summary
    var totalIncomeRow = CreateSummaryRow("Total Income", (month) => CalculateTotals(incomeEnvelopes, month));
    _summaryRows.Add(totalIncomeRow);
    _displayRows.Add(totalIncomeRow);

    // Add Total Expenses to summary
    var totalExpensesRow = CreateSummaryRow("Total Expenses", (month) => CalculateTotals(expenseEnvelopes, month));
    _summaryRows.Add(totalExpensesRow);
    _displayRows.Add(totalExpensesRow);

    // Add Net Budget row to summary
    var netBudgetRow = CreateSummaryRow("Net Budget", (month) =>
    {
      var (budget, draft) = CalculateTotals(incomeEnvelopes, month);
      var expenseTotals = CalculateTotals(expenseEnvelopes, month);
      return (budget - expenseTotals.budget, draft - expenseTotals.draft);
    });
    _summaryRows.Add(netBudgetRow);
    _displayRows.Add(netBudgetRow);

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
      CategoryId = envelope.CategoryId,
      CategoryName = envelope.CategoryName,
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
          DraftDisplayValue = data.DraftValue?.ToString("C2") ?? string.Empty,
          IsLocked = data.IsBudgetLocked
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

  /// <summary>
  /// Update the draft amount for a specific envelope and month, persist the change to the backend, and refresh the displayed rows.
  /// </summary>
  /// <param name="draftValue">New draft amount for the month, or null to clear any existing draft.</param>
  private async Task UpdateDraft(int envelopeId, DateTime month, decimal? draftValue)
  {
    try
    {
      var acctPeriod = AcctPeriodHelper.DateToAcctPeriod(month);
      var response = await BudgetMonthlyApi.UpdateBudgetDraftAsync(acctPeriod, envelopeId, draftValue);

      if (response.Success)
      {
        // Update local data
        if (_budgetData!.TryGetValue(envelopeId, out Dictionary<DateTime, BudgetMonthData>? value) &&
            value.TryGetValue(month, out BudgetMonthData? value1))
        {
          value1.DraftValue = draftValue;
        }

        // Update the specific cell in the display rows instead of rebuilding everything
        // This preserves focus by not destroying and recreating the DOM
        var envelopeRow = _envelopeRows.FirstOrDefault(r => r.EnvelopeId == envelopeId);
        if (envelopeRow != null && envelopeRow.MonthlyData.TryGetValue(month, out var cellData))
        {
          cellData.DraftValue = draftValue;
          cellData.DraftDisplayValue = draftValue?.ToString("C2") ?? string.Empty;
          // Increment update counter to force component recreation with @key
          cellData.UpdateCounter++;
        }

        BuildDisplayRows();
        // Force a re-render to update the formatted display
        // This will show the currency format (e.g., $123.00) without disrupting focus
        await InvokeAsync(StateHasChanged);
      }
      else
      {
        // Validation error - show message and prevent navigation
        // Using InvokeAsync to ensure UI thread
        await InvokeAsync(() => { Snackbar.Add(response.Message ?? "Validation error", Severity.Warning); });

        // Set a flag that JavaScript can check to prevent navigation
        await JsRuntime.InvokeVoidAsync("setValidationError", true);
      }
    }
    catch (Exception ex)
    {
      Snackbar.Add($"Error updating draft: {ex.Message}", Severity.Error);

      // Set validation error flag to prevent navigation
      await JsRuntime.InvokeVoidAsync("setValidationError", true);
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
      MonthsToShow++;
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
            IsBudgetLocked = item.IsBudgetLocked,
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

  /// <summary>
  /// Prompts the user to confirm clearing all draft budgets and, if confirmed, clears them and refreshes the budget data.
  /// </summary>
  /// <remarks>
  /// When confirmed, calls the API to clear draft budgets, displays a success or error notification, and reloads budget data without re-checking for drafts. The method updates the component's processing state while the operation runs.
  /// </remarks>
  private async Task ClearDrafts()
  {
    var parameters = new DialogParameters
    {
      ["Message"] = "Are you sure you want to clear all draft budgets? This action cannot be undone."
    };

    var options = new DialogOptions { CloseOnEscapeKey = true };
    var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm Clear Drafts", parameters, options);
    var result = await dialog.Result;

    if (result is { Canceled: false, Data: true })
    {
      try
      {
        _processing = true;
        StateHasChanged();

        var response = await BudgetMonthlyApi.ClearDraftBudgetsAsync();

        if (response.Success)
        {
          Snackbar.Add("Draft budgets cleared successfully", Severity.Success);
          await LoadBudgetData(false);
        }
      }
      catch (Exception ex)
      {
        Snackbar.Add($"Error clearing drafts: {ex.Message}", Severity.Error);
      }
      finally
      {
        _processing = false;
        StateHasChanged();
      }
    }
  }

  /// <summary>
  /// Prompts the user to confirm applying all draft budgets and, if confirmed, applies them via the API and reloads budget data.
  /// </summary>
  /// <remarks>
  /// On confirmation this method sets the component into a processing state, calls the API to apply draft values to the budget,
  /// shows a success or error message, and reloads budget data without re-checking for drafts. The component processing flag
  /// and UI state are restored when the operation completes or fails.
  /// </remarks>
  private async Task ApplyDrafts()
  {
    var parameters = new DialogParameters
    {
      ["Message"] = "Are you sure you want to apply all draft budgets? This will update all budget values."
    };

    var options = new DialogOptions { CloseOnEscapeKey = true };
    var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm Apply Budgets", parameters, options);
    var result = await dialog.Result;

    if (result is { Canceled: false, Data: true })
    {
      try
      {
        _processing = true;
        StateHasChanged();

        var response = await BudgetMonthlyApi.ApplyDraftValuesToBudgetAsync();

        if (response.Success)
        {
          Snackbar.Add("Draft budgets applied successfully", Severity.Success);
          await LoadBudgetData(false);
        }
      }
      catch (Exception ex)
      {
        Snackbar.Add($"Error applying drafts: {ex.Message}", Severity.Error);
      }
      finally
      {
        _processing = false;
        StateHasChanged();
      }
    }
  }

  /// <summary>
  /// Copies budget values from the specified displayed month into the next month.
  /// </summary>
  /// <param name="monthIndex">Zero-based index into the component's displayed months identifying the source month.</param>
  /// <param name="copyFromDraft">When true, copies draft values; otherwise copies committed budget values.</param>
  /// <remarks>
  /// If the operation would overwrite data in the target month, a confirmation dialog is shown before proceeding.
  /// Displays success or error notifications and reloads budget data (without re-checking drafts) after a successful copy.
  /// </remarks>
  private async Task CopyToNextMonth(int monthIndex, bool copyFromDraft)
  {
    try
    {
      _processing = true;
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
      if (response is { Success: false, WouldOverwriteData: true })
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
            dialogResult.Data is false)
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
      await LoadBudgetData(false);
    }
    catch (Exception ex)
    {
      Snackbar.Add($"Error copying to next month: {ex.Message}", Severity.Error);
    }
    finally
    {
      _processing = false;
    }
  }

  /// <summary>
  /// Toggle the budget lock state for a specific envelope and month, synchronizing the change with the server and updating local state and UI.
  /// </summary>
  /// <param name="envelopeId">Identifier of the envelope whose lock state should be toggled.</param>
  /// <param name="month">The month for which to toggle the lock.</param>
  private async Task ToggleLock(int envelopeId, DateTime month)
  {
    // Find the row in envelope rows (not summary rows)
    var row = _envelopeRows.FirstOrDefault(r => r.EnvelopeId == envelopeId);
    if (row != null && row.MonthlyData.TryGetValue(month, out MonthCellData? cellData))
    {
      var newLockState = !cellData.IsLocked;

      try
      {
        var acctPeriod = AcctPeriodHelper.DateToAcctPeriod(month);

        // If locking and there's a draft value, clear it first
        if (newLockState && cellData.DraftValue.HasValue)
        {
          var clearDraftResponse = await BudgetMonthlyApi.UpdateBudgetDraftAsync(acctPeriod, envelopeId, null);
          if (!clearDraftResponse.Success)
          {
            Snackbar.Add($"Error clearing draft: {clearDraftResponse.Message}", Severity.Error);
            return;
          }
        }

        var response = await BudgetMonthlyApi.UpdateBudgetLockAsync(acctPeriod, envelopeId, newLockState);

        if (response.Success)
        {
          // Update local state
          cellData.IsLocked = newLockState;

          // If we just locked, also clear the draft value locally
          if (newLockState)
          {
            cellData.DraftValue = null;
            cellData.DraftDisplayValue = string.Empty;
          }

          // Also update the underlying data
          if (_budgetData!.TryGetValue(envelopeId, out Dictionary<DateTime, BudgetMonthData>? value) &&
              value.TryGetValue(month, out BudgetMonthData? data))
          {
            data.IsBudgetLocked = newLockState;
            if (newLockState)
            {
              data.DraftValue = null;
            }
          }

          StateHasChanged();
        }
        else
        {
          Snackbar.Add($"Error updating lock: {response.Message}", Severity.Error);
        }
      }
      catch (Exception ex)
      {
        Snackbar.Add($"Error updating lock: {ex.Message}", Severity.Error);
      }
    }
  }

  // Data models
  private class BudgetDisplayRow
  {
    public int EnvelopeId { get; init; }
    public string CategoryName { get; set; }
    public string EnvelopeName { get; init; } = string.Empty;
    public bool IsSummaryRow { get; set; }
    public Dictionary<DateTime, MonthCellData> MonthlyData { get; init; } = [];
    public string CategoryId { get; set; }
  }

  private class MonthCellData
  {
    public decimal? DraftValue { get; set; }
    public decimal? BudgetValue { get; init; }
    public string DraftDisplayValue { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public int UpdateCounter { get; set; }
  }

  private class BudgetMonthData
  {
    public int EnvelopeId { get; init; }
    public string EnvelopeName { get; init; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public CatTypes CategoryType { get; init; }
    public int SortOrder { get; init; }
    public decimal? BudgetValue { get; init; }
    public decimal? DraftValue { get; set; }
    public bool IsBudgetLocked { get; set; }
    public DateTime Month { get; set; }
  }

  private async Task ClearMonthBudgetValues(int monthIndex, bool clearBudget)
  {
    try
    {
      // Bounds check
      if (monthIndex < 0 || monthIndex >= _displayMonths.Count)
      {
        Snackbar.Add("Invalid month index", Severity.Error);
        return;
      }

      var month = _displayMonths[monthIndex];
      var acctPeriod = AcctPeriodHelper.DateToAcctPeriod(month);

      var itemType = clearBudget ? "budgets" : "drafts";
      var itemTypeCapitalized = clearBudget ? "Budgets" : "Drafts";
      var parameters = new DialogParameters
      {
        ["Message"] =
          $"Are you sure you want to clear all {itemType} for {month:MMMM yyyy}? This action cannot be undone."
      };

      var options = new DialogOptions { CloseOnEscapeKey = true };
      var dialog =
        await DialogService.ShowAsync<ConfirmationDialog>($"Confirm Clear {itemTypeCapitalized}", parameters, options);
      var result = await dialog.Result;

      if (result is { Canceled: false, Data: true })
      {
        _processing = true;
        StateHasChanged();

        if (clearBudget)
        {
          var response = await BudgetMonthlyApi.ClearMonthBudgetsAsync(acctPeriod);
          if (response.Success)
          {
            Snackbar.Add(response.Message, Severity.Success);
            await LoadBudgetData();
          }
          else
          {
            Snackbar.Add($"Error: {response.Message}", Severity.Error);
          }
        }
        else
        {
          var response = await BudgetMonthlyApi.ClearMonthDraftsAsync(acctPeriod);
          if (response.Success)
          {
            Snackbar.Add(response.Message, Severity.Success);
            await LoadBudgetData();
          }
          else
          {
            Snackbar.Add($"Error: {response.Message}", Severity.Error);
          }
        }

        _processing = false;
        StateHasChanged();
      }
    }
    catch (Exception ex)
    {
      var itemType = clearBudget ? "budgets" : "drafts";
      Snackbar.Add($"Error clearing {itemType}: {ex.Message}", Severity.Error);
      _processing = false;
      StateHasChanged();
    }
  }

  private async Task ClearMonthBoth(int monthIndex)
  {
    try
    {
      // Bounds check
      if (monthIndex < 0 || monthIndex >= _displayMonths.Count)
      {
        Snackbar.Add("Invalid month index", Severity.Error);
        return;
      }

      var month = _displayMonths[monthIndex];
      var acctPeriod = AcctPeriodHelper.DateToAcctPeriod(month);

      var parameters = new DialogParameters
      {
        ["Message"] =
          $"Are you sure you want to clear all budgets and drafts for {month:MMMM yyyy}? This action cannot be undone."
      };

      var options = new DialogOptions { CloseOnEscapeKey = true };
      var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm Clear Both", parameters, options);
      var result = await dialog.Result;

      if (result is { Canceled: false, Data: true })
      {
        _processing = true;
        StateHasChanged();

        var response = await BudgetMonthlyApi.ClearMonthBothAsync(acctPeriod);

        if (response.Success)
        {
          Snackbar.Add(response.Message, Severity.Success);
          await LoadBudgetData();
        }
        else
        {
          Snackbar.Add($"Error: {response.Message}", Severity.Error);
        }

        _processing = false;
        StateHasChanged();
      }
    }
    catch (Exception ex)
    {
      Snackbar.Add($"Error clearing budgets and drafts: {ex.Message}", Severity.Error);
      _processing = false;
      StateHasChanged();
    }
  }

  private async Task ApplyMonthDrafts(int monthIndex)
  {
    try
    {
      // Bounds check
      if (monthIndex < 0 || monthIndex >= _displayMonths.Count)
      {
        Snackbar.Add("Invalid month index", Severity.Error);
        return;
      }

      var month = _displayMonths[monthIndex];
      var acctPeriod = AcctPeriodHelper.DateToAcctPeriod(month);

      var parameters = new DialogParameters
      {
        ["Message"] =
          $"Are you sure you want to copy all draft values to budgets for {month:MMMM yyyy}? This will update budget values."
      };

      var options = new DialogOptions { CloseOnEscapeKey = true };
      var dialog =
        await DialogService.ShowAsync<ConfirmationDialog>("Confirm Copy Drafts To Budgets", parameters, options);
      var result = await dialog.Result;

      if (result is { Canceled: false, Data: true })
      {
        _processing = true;
        StateHasChanged();

        var response = await BudgetMonthlyApi.ApplyMonthDraftsAsync(acctPeriod);

        if (response.Success)
        {
          Snackbar.Add(response.Message, Severity.Success);
          await LoadBudgetData();
        }
        else
        {
          Snackbar.Add($"Error: {response.Message}", Severity.Error);
        }

        _processing = false;
        StateHasChanged();
      }
    }
    catch (Exception ex)
    {
      Snackbar.Add($"Error applying drafts to budgets: {ex.Message}", Severity.Error);
      _processing = false;
      StateHasChanged();
    }
  }
}