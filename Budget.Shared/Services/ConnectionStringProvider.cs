namespace Budget.Shared.Services;

/// <summary>
/// Provides access to resolved connection strings for the application.
/// Connection strings are resolved once at startup and cached for the lifetime of the application.
/// </summary>
public sealed class ConnectionStringProvider : IConnectionStringProvider
{
  public string BudgetConnectionString { get; }
  public string IdentityConnectionString { get; }
  public bool UseAzureDatabase { get; }

  /// <summary>
  /// Initializes the connection string provider with pre-resolved connection strings
  /// </summary>
  public ConnectionStringProvider(string budgetConnectionString, string identityConnectionString, bool useAzureDatabase)
  {
    if (string.IsNullOrWhiteSpace(budgetConnectionString))
      throw new ArgumentException("Budget connection string cannot be null or empty", nameof(budgetConnectionString));
    
    if (string.IsNullOrWhiteSpace(identityConnectionString))
      throw new ArgumentException("Identity connection string cannot be null or empty", nameof(identityConnectionString));

    BudgetConnectionString = budgetConnectionString;
    IdentityConnectionString = identityConnectionString;
    UseAzureDatabase = useAzureDatabase;
  }

  /// <summary>
  /// Factory method to create ConnectionStringProvider from WebApplicationBuilder
  /// </summary>
  public static ConnectionStringProvider Create(WebApplicationBuilder builder, ILogger logger)
  {
    logger.LogInformation("Resolving connection strings for ConnectionStringProvider");
    
    var useAzureDb = Misc.UseAzureDB(builder, logger);
    var budgetConnectionString = Misc.GetConnectionString(builder, Misc.ConnectionStringType.Budget, logger);
    var identityConnectionString = Misc.GetConnectionString(builder, Misc.ConnectionStringType.Identity, logger);

    logger.LogInformation("Connection strings resolved successfully");
    logger.LogInformation("  - Budget DB: {DataSource} (Azure: {UseAzure})", 
      Misc.ParseDataSource(budgetConnectionString) ?? "unknown", useAzureDb);
    logger.LogInformation("  - Identity DB: {DataSource} (Azure: {UseAzure})", 
      Misc.ParseDataSource(identityConnectionString) ?? "unknown", useAzureDb);

    return new ConnectionStringProvider(budgetConnectionString, identityConnectionString, useAzureDb);
  }
}
