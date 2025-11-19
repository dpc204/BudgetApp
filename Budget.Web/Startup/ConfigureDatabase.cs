using Budget.DB;
using Budget.Shared;
using Budget.Web.Data;
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
  public static void AddDatabaseContexts(WebApplicationBuilder builder)
  {
    var budgetConnectionString = Misc.GetConnectionString(builder.Configuration, Misc.ConnectionStringType.Budget);

    builder.Services.AddDbContext<BudgetContext>((sp, options) =>
    {
      var env = sp.GetRequiredService<IHostEnvironment>();
      options.UseSqlServer(budgetConnectionString);
      if (env.IsDevelopment())
      {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
      }
    });

    builder.Services.AddQuickGridEntityFrameworkAdapter();

    // Use the SAME database for Identity as BudgetContext (Identity schema within the same DB)
    builder.Services.AddDbContext<IdentityDBContext>(options =>
      options.UseSqlServer(budgetConnectionString,
        o => o.MigrationsHistoryTable("__EFMigrationsHistory", "BudgetIdentity")));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
  }

  /// <summary>
  /// Applies Identity database migrations and logs startup information
  /// </summary>
  public static void ApplyMigrations(WebApplication app, string budgetConnectionString)
  {
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogInformation(
      "Application starting at {UtcTime} with BudgetDB host parsed from connection string: {DataSource}",
      DateTime.UtcNow,
      Misc.ParseDataSource(budgetConnectionString));

    startupLogger.LogInformation(
      "Application starting at {UtcTime} with IdentityDB host parsed from connection string: {DataSource}",
      DateTime.UtcNow,
      Misc.ParseDataSource(budgetConnectionString));

    // Ensure Identity schema is created/migrated so 'BudgetIdentity.AspNetUsers' exists
    using var scope = app.Services.CreateScope();
    try
    {
      var idDb = scope.ServiceProvider.GetRequiredService<IdentityDBContext>();
      idDb.Database.Migrate();
    }
    catch (Exception ex)
    {
      var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
      logger.LogError(ex, "Error applying IdentityDBContext migrations");
      throw;
    }
  }
}
