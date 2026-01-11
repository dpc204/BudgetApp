namespace Budget.Client.Services;

/// <summary>
/// Service for calculating fund allocation amounts based on budget and fill strategies
/// </summary>
public class FundAllocationService : IFundAllocationService
{
  /// <summary>
  /// Calculates the fund amount for a single envelope based on the fill type
  /// </summary>
  /// <param name="budget">The envelope's budget amount</param>
  /// <param name="currentBalance">The envelope's current balance</param>
  /// <param name="fillType">The fill strategy to apply</param>
  /// <returns>The calculated fund amount, or null if no budget is set</returns>
  public decimal? CalculateFundAmount(decimal? budget, decimal currentBalance, FillAmounts fillType)
  {
    if (!budget.HasValue)
      return null;

    var budgetAmount = budget.Value;

    return fillType switch
    {
      FillAmounts.OneHundredPercent => budgetAmount,
      FillAmounts.FiftyPercent => budgetAmount * 0.5m,
      FillAmounts.FillToBudget => CalculateFillToBudget(budgetAmount, currentBalance),
      FillAmounts.NotSet => null,
      _ => throw new ArgumentOutOfRangeException(nameof(fillType), fillType, "Unknown fill type")
    };
  }

  /// <summary>
  /// Calculates fund amounts for multiple envelopes based on the fill type
  /// </summary>
  /// <param name="envelopes">Collection of envelopes with budget and balance information</param>
  /// <param name="fillType">The fill strategy to apply</param>
  /// <returns>Dictionary mapping envelope IDs to their calculated fund amounts</returns>
  public Dictionary<int, decimal?> CalculateFundAmounts<T>(
    IEnumerable<T> envelopes,
    FillAmounts fillType) where T : IFundableEnvelope
  {
    var results = new Dictionary<int, decimal?>();

    foreach (var envelope in envelopes)
    {
      if (envelope.Budget.HasValue)
      {
        results[envelope.EnvelopeId] = CalculateFundAmount(
          envelope.Budget,
          envelope.CurrentBalance,
          fillType);
      }
    }

    return results;
  }

  /// <summary>
  /// Calculates the amount needed to bring the envelope balance up to its budget
  /// </summary>
  private static decimal CalculateFillToBudget(decimal budget, decimal currentBalance)
  {
    // If current balance is already at or above budget, return 0
    if (currentBalance >= budget)
      return 0m;

    // Otherwise, return the difference needed to reach budget
    return budget - currentBalance;
  }
}

/// <summary>
/// Interface for fund allocation calculations
/// </summary>
public interface IFundAllocationService
{
  /// <summary>
  /// Calculates the fund amount for a single envelope based on the fill type
  /// </summary>
  decimal? CalculateFundAmount(decimal? budget, decimal currentBalance, FillAmounts fillType);

  /// <summary>
  /// Calculates fund amounts for multiple envelopes based on the fill type
  /// </summary>
  Dictionary<int, decimal?> CalculateFundAmounts<T>(IEnumerable<T> envelopes, FillAmounts fillType)
    where T : IFundableEnvelope;
}

/// <summary>
/// Interface for envelopes that can be funded
/// </summary>
public interface IFundableEnvelope
{
  int EnvelopeId { get; }
  decimal? Budget { get; }
  decimal CurrentBalance { get; }
}
