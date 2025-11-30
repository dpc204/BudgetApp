namespace Budget.Shared.Utilities;

/// <summary>
/// Utility methods for converting between DateTime and AcctPeriod formats
/// </summary>
public static class AcctPeriodHelper
{
  /// <summary>
  /// Converts a DateTime to AcctPeriod format (YYYYMM)
  /// </summary>
  public static int DateToAcctPeriod(DateTime date)
  {
    return date.Year * 100 + date.Month;
  }

  /// <summary>
  /// Converts AcctPeriod format (YYYYMM) to DateTime (first of month)
  /// </summary>
  public static DateTime AcctPeriodToDate(int acctPeriod)
  {
    var year = acctPeriod / 100;
    var month = acctPeriod % 100;
    
    if (month < 1 || month > 12)
    {
      throw new ArgumentException($"Invalid AcctPeriod format: {acctPeriod}. Month must be between 1-12.", nameof(acctPeriod));
    }
    
    return new DateTime(year, month, 1);
  }
}
