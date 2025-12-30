using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Budget.Api;
using Budget.DB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
  protected HttpClient Client => _factory.CreateClient();
  protected readonly WebApplicationFactory<Program> _factory;
  private readonly string _budgetDbName;
  private readonly string _identityDbName;

  public IntegrationTestBase()
  {
    _budgetDbName = Guid.NewGuid().ToString(); // Unique DB per test
    _identityDbName = Guid.NewGuid().ToString(); // Unique DB per test

    _factory = new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureAppConfiguration((context, config) =>
        {
          // Add in-memory configuration for test connection strings
          config.AddInMemoryCollection(new Dictionary<string, string?>
          {
            ["LocalBudgetConnection"] = "TestConnection",
            ["LocalIdentityConnection"] = "TestConnection",
            ["BudgetConnection"] = "TestConnection",
            ["IdentityConnection"] = "TestConnection"
          });
        });
        
        builder.ConfigureServices(services =>
        {
          // Remove all BudgetContext registrations
          services.RemoveAll<DbContextOptions<BudgetContext>>();
          services.RemoveAll<BudgetContext>();

          // Remove all ApiIdentityContext registrations
          services.RemoveAll<DbContextOptions<ApiIdentityContext>>();
          services.RemoveAll<ApiIdentityContext>();

          // Register in-memory DB for BudgetContext
          services.AddDbContext<BudgetContext>(options =>
            options.UseInMemoryDatabase(_budgetDbName));

          // Register in-memory DB for ApiIdentityContext
          services.AddDbContext<ApiIdentityContext>(options =>
            options.UseInMemoryDatabase(_identityDbName));
        });
      });

    // Seed the Family entity for tests
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      if (!db.Families.Any())
      {
        db.Families.Add(new Family { Id = 1, Name = "Test Family" });
        db.SaveChanges();
      }
    }
  }
}