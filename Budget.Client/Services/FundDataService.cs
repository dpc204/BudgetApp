namespace Budget.Client.Services;

/// <summary>
/// Service for loading and transforming fund data
/// </summary>
public class FundDataService(IBudgetMonthlyApiClient apiClient) : IFundDataService
{
  /// <summary>
  /// Loads fund data for the specified month
  /// </summary>
  /// <param name="year">The year to load data for</param>
  /// <param name="month">The month to load data for</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Fund data result containing envelopes and totals</returns>
  public async Task<FundDataResult> LoadFundDataAsync(int year, int month, CancellationToken cancellationToken = default)
  {
    var monthData = await apiClient.GetBudgetMonthAsync(year, month, cancellationToken);
    var allocateEnvelope = await apiClient.GetEnvelopeByEnvelopeTypeAsync(EnvelopeTypes.Unallocated, cancellationToken);

    var fundData = new Dictionary<int, FundEnvelopeData>();
    var totalBudget = 0m;
    var totalBalance = 0m;
    var availableToFund = allocateEnvelope.Balance;

    foreach (var item in monthData.Where(a => a.CategoryType == CatTypes.User))
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
        CurrentBalance = item.Balance,
        FundAmount = item.FundAmount
      };

      availableToFund -= envelopeData.FundAmount ?? 0m;
      fundData[item.EnvelopeId] = envelopeData;

      totalBudget += item.Budget ?? 0;
      totalBalance += item.Balance;
    }

    return new FundDataResult
    {
      FundData = fundData,
      TotalBudget = totalBudget,
      TotalBalance = totalBalance,
      AvailableToFund = availableToFund
    };
  }

  /// <summary>
  /// Builds display rows from fund data
  /// </summary>
  /// <param name="fundData">The fund data to transform</param>
  /// <returns>List of display rows sorted by sort order</returns>
  public List<FundDisplayRow> BuildDisplayRows(Dictionary<int, FundEnvelopeData> fundData)
  {
    if (fundData == null || fundData.Count == 0)
      return [];

    return fundData.Values
      .OrderBy(e => e.SortOrder)
      .Select(envelope => new FundDisplayRow
      {
        EnvelopeId = envelope.EnvelopeId,
        EnvelopeName = envelope.EnvelopeName,
        CurrentBalance = envelope.CurrentBalance,
        Budget = envelope.Budget,
        FundAmount = envelope.FundAmount,
        UpdateCounter = 0
      })
      .ToList();
  }
}

/// <summary>
/// Interface for fund data operations
/// </summary>
public interface IFundDataService
{
  /// <summary>
  /// Loads fund data for the specified month
  /// </summary>
  Task<FundDataResult> LoadFundDataAsync(int year, int month, CancellationToken cancellationToken = default);

  /// <summary>
  /// Builds display rows from fund data
  /// </summary>
  List<FundDisplayRow> BuildDisplayRows(Dictionary<int, FundEnvelopeData> fundData);
}

/// <summary>
/// Result of loading fund data
/// </summary>
public class FundDataResult
{
  public Dictionary<int, FundEnvelopeData> FundData { get; set; } = [];
  public decimal TotalBudget { get; set; }
  public decimal TotalBalance { get; set; }
  public decimal AvailableToFund { get; set; }
}

/// <summary>
/// Data model for a fund envelope
/// </summary>
public class FundEnvelopeData : IFundableEnvelope
{
  public int EnvelopeId { get; set; }
  public string EnvelopeName { get; set; } = string.Empty;
  public string CategoryId { get; set; } = string.Empty;
  public string CategoryName { get; set; } = string.Empty;
  public CatTypes CategoryType { get; set; }
  public int SortOrder { get; set; }
  public decimal? Budget { get; set; }
  public decimal CurrentBalance { get; set; }
  public decimal? FundAmount { get; set; }
}

/// <summary>
/// Display row for fund envelope in the UI
/// </summary>
public class FundDisplayRow
{
  public int EnvelopeId { get; set; }
  public string EnvelopeName { get; set; } = string.Empty;
  public decimal CurrentBalance { get; set; }
  public decimal? Budget { get; set; }
  public decimal? FundAmount { get; set; }
  public int UpdateCounter { get; set; }
}
