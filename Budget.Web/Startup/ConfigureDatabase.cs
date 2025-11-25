using System;
using Budget.DB;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

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


    var identityConnectionString = Misc.GetConnectionString(builder.Configuration, Misc.ConnectionStringType.Identity);


    // Use the SAME database for Identity as BudgetContext (Identity schema within the same DB)
    builder.Services.AddDbContext<IdentityDBContext>(options =>
      options.UseSqlServer(identityConnectionString,
        o => o.MigrationsHistoryTable("__EFMigrationsHistory", "BudgetIdentity")));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
  }

}
