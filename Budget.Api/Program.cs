using Scalar.AspNetCore;
using Budget.Api;
using Budget.Api.Services;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

using var loggerFactory = LoggerFactory.Create(loggingBuilder =>
{
  loggingBuilder
    .SetMinimumLevel(LogLevel.Information)
    .AddConsole();
});

ILogger logger = loggerFactory.CreateLogger<Program>();

// Log Azure environment detection
logger.LogInformation("=== Azure Environment Detection ===");
logger.LogInformation("IsRunningOnAzure: {IsAzure}", AzureEnvironment.IsRunningOnAzure);
logger.LogInformation("Hosting Environment: {Environment}", AzureEnvironment.HostingEnvironment);
logger.LogInformation("App Name: {AppName}", AzureEnvironment.AppName ?? "N/A");
logger.LogInformation("Instance ID: {InstanceId}", AzureEnvironment.InstanceId ?? "N/A");

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Configure logging
builder.Logging.AddJsonConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);

// Add HTTP logging
builder.Services.AddHttpLogging(logging =>
{
  logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
  logging.RequestBodyLogLimit = 4096;
  logging.ResponseBodyLogLimit = 4096;
});

// Add OpenAPI and Carter
builder.Services.AddOpenApi();
builder.Services.AddCarter();

var assembly = typeof(Budget.Api.Program).Assembly;

// Configure configuration sources (appsettings, secrets, environment, Key Vault)
Misc.SetupConfigurationSources(builder, assembly, logger);


// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetAll).Assembly));

// Register HttpContextAccessor and CurrentFamilyService for multi-tenancy
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentFamilyService, CurrentFamilyService>();

// Get connection strings
var budgetConnectionString = Misc.GetConnectionString(builder, Misc.ConnectionStringType.Budget, logger);
var identityConnectionString = Misc.GetConnectionString(builder, Misc.ConnectionStringType.Identity, logger);

if (string.IsNullOrWhiteSpace(budgetConnectionString))
  throw new InvalidOperationException("Missing Budget DB connection string.");
if (string.IsNullOrWhiteSpace(identityConnectionString))
  throw new InvalidOperationException("Missing Identity DB connection string.");

var isDev = builder.Environment.IsDevelopment();
//var isTest = builder.Environment.IsEnvironment("Testing") || builder.Environment.IsEnvironment("Test");
var isTest = AppDomain.CurrentDomain.GetAssemblies()
  .Any(a => a.FullName != null && (a.FullName.StartsWith("xunit")
                                   || a.FullName.StartsWith("nunit")
                                   || a.FullName.StartsWith("Microsoft.VisualStudio.TestPlatform")));

// Configure BudgetContext
builder.Services.AddDbContext<BudgetContext>(options =>
{
  if (isTest)
  {
    options.UseInMemoryDatabase("BudgetTestDb");
  }
  else
  {
    options.UseSqlServer(budgetConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "budget")
        .EnableRetryOnFailure(
          maxRetryCount: 5, // Number of retries
          maxRetryDelay: TimeSpan.FromSeconds(10), // Delay between retries
          errorNumbersToAdd: null) // Additional SQL error codes to retry on)
    );
  }

  if (isDev || isTest)
  {
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
  }
});

// Configure ApiIdentityContext
builder.Services.AddDbContext<ApiIdentityContext>(options =>
{
  if (isTest)
  {
    options.UseInMemoryDatabase("IdentityTestDb");
  }
  else
  {
    options.UseSqlServer(identityConnectionString, options => options.EnableRetryOnFailure(
      maxRetryCount: 5, // Number of retries
      maxRetryDelay: TimeSpan.FromSeconds(10), // Delay between retries
      errorNumbersToAdd: null));
  }

  if (isDev || isTest)
  {
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
  }
});

// Configure Identity
builder.Services
  .AddIdentityCore<BudgetUser>(o => { o.User.RequireUniqueEmail = true; })
  .AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<ApiIdentityContext>()
  .AddSignInManager();

// Configure JWT authentication
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOpt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpt.SigningKey));

// Configure dual authentication: Entra ID JWT + Custom JWT
// Use explicit scheme names to avoid conflicts
const string EntraScheme = "EntraJwt";
const string LocalScheme = "LocalJwt";

var authBuilder = builder.Services.AddAuthentication(options =>
{
  // Default to Entra ID if configured, otherwise fallback to local JWT
  var azureAdSection = builder.Configuration.GetSection("AzureAd");
  if (!string.IsNullOrWhiteSpace(azureAdSection["ClientId"]))
  {
    options.DefaultScheme = EntraScheme;
    options.DefaultChallengeScheme = EntraScheme;
    logger.LogInformation("Default authentication scheme: {Scheme}", EntraScheme);
  }
  else
  {
    options.DefaultScheme = LocalScheme;
    options.DefaultChallengeScheme = LocalScheme;
    logger.LogInformation("Default authentication scheme: {Scheme} (Entra ID not configured)", LocalScheme);
  }
});

// Add custom JWT Bearer for backward compatibility (local auth)
authBuilder.AddJwtBearer(LocalScheme, options =>
{
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtOpt.Issuer,
    ValidAudience = jwtOpt.Audience,
    IssuerSigningKey = key,
    ClockSkew = TimeSpan.FromMinutes(1)
  };
});

// Add Microsoft Entra ID JWT Bearer authentication
var azureAdSection = builder.Configuration.GetSection("AzureAd");
if (!string.IsNullOrWhiteSpace(azureAdSection["ClientId"]))
{
  authBuilder.AddMicrosoftIdentityWebApi(azureAdSection, EntraScheme);
  logger.LogInformation("Microsoft Entra ID JWT Bearer authentication configured with scheme: {Scheme}", EntraScheme);
}
else
{
  logger.LogWarning("AzureAd:ClientId not configured - Entra ID authentication disabled");
}

// Configure authorization policies matching Budget.Web
builder.Services.AddAuthorization(options =>
{
  // Default policy requires authentication from either scheme
  options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .AddAuthenticationSchemes(EntraScheme, LocalScheme)
    .Build();

  // Admin-only policy
  options.AddPolicy("AdminOnly", policy => policy
    .RequireRole("Admin")
    .AddAuthenticationSchemes(EntraScheme, LocalScheme));

  // PowerUser or above policy
  options.AddPolicy("PowerUserOrAbove", policy => policy
    .RequireAssertion(context =>
      context.User.IsInRole("Admin") || context.User.IsInRole("PowerUser"))
    .AddAuthenticationSchemes(EntraScheme, LocalScheme));

  // Authenticated user policy (any role)
  options.AddPolicy("AuthenticatedUser", policy => policy
    .RequireAuthenticatedUser()
    .AddAuthenticationSchemes(EntraScheme, LocalScheme));
});

// Register JWT token service
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Register BackupAzureSql service with HttpClient
builder.Services.AddHttpClient<BackupAzureSql>();
var app = builder.Build();

// Ensure databases exist
using (var scope = app.Services.CreateScope())
{
  var services = scope.ServiceProvider;
  services.GetRequiredService<ApiIdentityContext>().Database.EnsureCreated();
  services.GetRequiredService<BudgetContext>().Database.EnsureCreated();
}

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
  app.UseHttpLogging();
  app.MapOpenApi();
  app.MapScalarApiReference(options =>
    options.WithTheme(ScalarTheme.DeepSpace)
      .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map Carter endpoints
app.MapCarter();

// Map default Aspire endpoints (health checks)
app.MapDefaultEndpoints();

// Sample weather forecast endpoint
var summaries = new[]
  { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

app.MapGet("/weatherforecast", () =>
{
  var forecast = Enumerable.Range(1, 5)
    .Select(index => new WeatherForecast(
      DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
      Random.Shared.Next(-20, 55),
      summaries[Random.Shared.Next(summaries.Length)]))
    .ToArray();
  return forecast;
}).WithName("GetWeatherForecast");

app.Run();

// Program class for WebApplicationFactory in tests
namespace Budget.Api
{
  public partial class Program
  {
  }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}