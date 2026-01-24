using System.IO;
using System.Net.Http;
using System.Reflection;
using Budget.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;


namespace Budget.ApiTests;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
  //protected HttpClient Client => _factory.CreateClient();
  //protected readonly WebApplicationFactory<Program> _factory;
  //private readonly string _budgetDbName;
  //private readonly string _identityDbName;

  //public IntegrationTestBase()
  //{
  //  _budgetDbName = Guid.NewGuid().ToString(); // Unique DB per test
  //  _identityDbName = Guid.NewGuid().ToString(); // Unique DB per test

  //  _factory = new WebApplicationFactory<Program>()
  //    .WithWebHostBuilder(builder =>
  //    {
  //      builder.ConfigureAppConfiguration((context, config) =>
  //      {
  //        // Add in-memory configuration for test connection strings
  //        config.AddInMemoryCollection(new Dictionary<string, string?>
  //        {
  //          ["LocalBudgetConnection"] = "TestConnection",
  //          ["LocalIdentityConnection"] = "TestConnection",
  //          ["BudgetConnection"] = "TestConnection",
  //          ["IdentityConnection"] = "TestConnection"
  //        });
  //      });

  //      builder.ConfigureServices(services =>
  //      {
  //        // Remove all BudgetContext registrations
  //        services.RemoveAll<DbContextOptions<BudgetContext>>();
  //        services.RemoveAll<BudgetContext>();

  //        // Remove all ApiIdentityContext registrations
  //        services.RemoveAll<DbContextOptions<ApiIdentityContext>>();
  //        services.RemoveAll<ApiIdentityContext>();

  //        // Register in-memory DB for BudgetContext
  //        services.AddDbContext<BudgetContext>(options =>
  //          options.UseInMemoryDatabase(_budgetDbName));

  //        // Register in-memory DB for ApiIdentityContext
  //        services.AddDbContext<ApiIdentityContext>(options =>
  //          options.UseInMemoryDatabase(_identityDbName));

  //        // Remove existing authentication services
  //        services.RemoveAll<IAuthenticationService>();
  //        services.RemoveAll<IAuthenticationSchemeProvider>();
  //        services.RemoveAll<IAuthenticationHandlerProvider>();

  //        // Override authentication with test scheme that always succeeds
  //        services.AddAuthentication(options =>
  //        {
  //          options.DefaultScheme = "TestScheme";
  //          options.DefaultAuthenticateScheme = "TestScheme";
  //          options.DefaultChallengeScheme = "TestScheme";
  //        })
  //        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

  //        // Override authorization to always succeed
  //        services.AddAuthorization(options =>
  //        {
  //          options.DefaultPolicy = new AuthorizationPolicyBuilder()
  //            .RequireAssertion(_ => true) // Always authorize
  //            .Build();
  //        });
  //      });
  //    });

  //  // Seed the Family entity for tests
  //  using var scope = _factory.Services.CreateScope();
  //  var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
  //  if (!db.Families.Any())
  //  {
  //    db.Families.Add(new Family { Id = 1, Name = "Test Family" });
  //    db.SaveChanges();
  //  }
  //}


  public  DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {

    var bld = new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}");

    bld.ConfigureWarnings(warn => warn.Ignore(InMemoryEventId.TransactionIgnoredWarning));

return bld.Options;
  }

protected  BudgetContext GetTestDBContext(int familyId = 1)
  {
    var db = new BudgetContext(CreateInMemoryOptions(), new TestCurrentFamilyService(familyId));
    
    
    return db;
  }

  private class TestCurrentFamilyService(int familyId = 1) : ICurrentFamilyService
  {
    public int FamilyId { get; set; } = familyId;
    public int GetCurrentFamilyId() => FamilyId;
  }

  public class UserSecretsReader
  {
    public static string GetSecret(string key)
    {
      // Locate the user secrets file
      var userSecretsId = typeof(UserSecretsReader).Assembly.GetCustomAttribute<UserSecretsIdAttribute>().UserSecretsId;
      if(string.IsNullOrEmpty(userSecretsId))
      {
        throw new InvalidOperationException("User Secrets ID is not defined.");
      }
      var secretsPath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
      // Load the secrets file
      var configuration = new ConfigurationBuilder()
        .AddJsonFile(secretsPath)
        .Build();
      // Retrieve the secret value by key
      return configuration[key];
    }
  }
  public static class PathHelper
  {
    public static string GetSecretsPathFromSecretsId(string userSecretsId)
    {
      var userSecretsRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Microsoft",
        "UserSecrets");
      return Path.Combine(userSecretsRoot, userSecretsId, "secrets.json");
    }
  }

}