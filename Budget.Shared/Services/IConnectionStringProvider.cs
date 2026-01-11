namespace Budget.Shared.Services;

/// <summary>
/// Provides access to resolved connection strings for the application
/// </summary>
public interface IConnectionStringProvider
{
  /// <summary>
  /// Gets the Budget database connection string
  /// </summary>
  string BudgetConnectionString { get; }

  /// <summary>
  /// Gets the Identity database connection string
  /// </summary>
  string IdentityConnectionString { get; }

  /// <summary>
  /// Indicates whether Azure database is being used
  /// </summary>
  bool UseAzureDatabase { get; }
}
