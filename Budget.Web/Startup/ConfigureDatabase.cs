using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Startup;

/// <summary>
/// Configures database contexts and handles migrations
/// </summary>
public static class ConfigureDatabase
{
  /// <summary>
  /// Adds database contexts for Budget and Identity
  /// </summary>
  public static void AddDatabaseContexts(WebApplicationBuilder builder, ILogger logger)
  {
    //var budgetConnectionString = Misc.GetConnectionString(builder.Configuration, Misc.ConnectionStringType.Budget);

    //builder.Services.AddDbContext<BudgetContext>((sp, options) =>
    //{
    //  var env = sp.GetRequiredService<IHostEnvironment>();
    //  options.UseSqlServer(budgetConnectionString);
    //  if (env.IsDevelopment())
    //  {
    //    options.EnableDetailedErrors();
    //    options.EnableSensitiveDataLogging();
    //  }
    //});

    // TEMPORARY: During Phase 2 Entra ID migration, Identity database is optional
    // The database will be fully removed in Phase 4 after user migration is complete
    try
    {
      var identityConnectionString = Misc.GetConnectionString(builder, Misc.ConnectionStringType.Identity, logger);

      // Use the SAME database for Identity as BudgetContext (Identity schema within the same DB)
      builder.Services.AddDbContext<IdentityDBContext>(options =>
        options.UseSqlServer(identityConnectionString,
          o => o.MigrationsHistoryTable("__EFMigrationsHistory", "BudgetIdentity").EnableRetryOnFailure(
            maxRetryCount: 5, // Number of retries
            maxRetryDelay: TimeSpan.FromSeconds(10), // Delay between retries
            errorNumbersToAdd: null)));

      builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Connection string"))
    {
      // During Entra ID migration, Identity database is optional
      logger.LogWarning("Identity database connection string not found. Skipping Identity database configuration during Entra ID migration. Error: {Error}", ex.Message);
      
      // Add a dummy/in-memory context for components that still reference it during migration
      builder.Services.AddDbContext<IdentityDBContext>(options =>
        options.UseInMemoryDatabase("TemporaryIdentityDb"));
    }
  }

}
