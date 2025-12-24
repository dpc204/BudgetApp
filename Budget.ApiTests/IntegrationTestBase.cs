using System;
using System.Linq;
using System.Net.Http;
using Budget.Api;
using Budget.DB;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
  }
}