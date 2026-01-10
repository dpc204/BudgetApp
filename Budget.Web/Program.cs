var stopWatch = Stopwatch.StartNew();

var builder = WebApplication.CreateBuilder(args);
var assembly = typeof(App).Assembly;

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

// Configure configuration sources (appsettings, secrets, environment, Key Vault)
Misc.SetupConfigurationSources(builder, assembly, logger);

// Register connection string provider as a singleton
var connectionStringProvider = ConnectionStringProvider.Create(builder, logger);
builder.Services.AddSingleton<IConnectionStringProvider>(connectionStringProvider);

// Configure culture for consistent currency formatting across environments
ConfigureServices.ConfigureGlobalization(builder);

// Configure telemetry (logging, metrics, tracing, health checks, service discovery)
AddTelemetry.ConfigureTelemetryAndServiceDefaults(builder);

// Configure logging
ConfigureServices.ConfigureLogging(builder);

// Configure Kestrel server limits
ConfigureServices.ConfigureKestrel(builder);

// Add Blazor services
ConfigureServices.AddBlazorServices(builder);

// Add HTTP clients for API communication
ConfigureServices.AddHttpClients(builder);

// Add UI libraries (MudBlazor, Syncfusion)
ConfigureServices.AddUILibraries(builder);

// Add application services
ConfigureServices.AddApplicationServices(builder);

// Add authentication and authorization
ConfigureIdentity.AddAuthentication(builder);

ConfigureIdentity.AddAuthorization(builder);

// Add database contexts
ConfigureDatabase.AddDatabaseContexts(builder, logger);

// Add Identity services
ConfigureIdentity.AddIdentityCore(builder);

var app = builder.Build();

Misc.LogAllConfigurationSettings(builder, logger);

// Clear token cache on startup to prevent stale token issues
using (var scope = app.Services.CreateScope())
{
  var tokenCacheManager = scope.ServiceProvider.GetRequiredService<Budget.Web.Services.TokenCacheManager>();
  await tokenCacheManager.ClearCacheOnStartupAsync();
}

// Initialize ServiceAccessor with built service provider for parameterless constructors
ServiceAccessor.Configure(app.Services);

// Apply database migrations and log startup info
// TEMPORARY: During Phase 2 Entra ID migration, Identity connection string is optional
try
{
  var budgetConnectionString = Misc.GetConnectionString(builder, Misc.ConnectionStringType.Identity, logger);
  logger.LogInformation("Identity database connection string configured: {ConnectionString}", budgetConnectionString?.Substring(0, Math.Min(50, budgetConnectionString?.Length ?? 0)) + "...");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("Connection string"))
{
  logger.LogWarning("Identity database connection string not configured. Continuing with Entra ID authentication only.");
}

// Configure exception handling
ConfigureMiddleware.ConfigureExceptionHandling(app);

// Configure static files
ConfigureMiddleware.ConfigureStaticFiles(app);

// Configure middleware pipeline
ConfigureMiddleware.ConfigurePipeline(app);

// Map endpoints
ConfigureMiddleware.MapEndpoints(app);

logger.LogInformation("Program Startup Time: {time}ms", stopWatch.ElapsedMilliseconds);
Debug.WriteLine($"Program Startup: {stopWatch.ElapsedMilliseconds}");
app.Run();

