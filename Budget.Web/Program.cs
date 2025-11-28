using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

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

// Initialize ServiceAccessor with built service provider for parameterless constructors
ServiceAccessor.Configure(app.Services);

// Apply database migrations and log startup info
var budgetConnectionString = Misc.GetConnectionString(builder, Misc.ConnectionStringType.Identity, logger);

// Configure exception handling
ConfigureMiddleware.ConfigureExceptionHandling(app);

// Configure static files
ConfigureMiddleware.ConfigureStaticFiles(app);

// Configure middleware pipeline
ConfigureMiddleware.ConfigurePipeline(app);

// Map endpoints
ConfigureMiddleware.MapEndpoints(app);

app.Run();

