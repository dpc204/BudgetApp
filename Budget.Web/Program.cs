using Azure.Identity;
using Budget.Api;
using Budget.Shared;
using Budget.Shared.Services;
using Budget.Web.Components;
using Budget.Web.Startup;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var assembly = typeof(App).Assembly;

// Configure configuration sources (appsettings, secrets, environment, Key Vault)
Misc.SetupConfigurationSources(builder, assembly);

// Configure telemetry (logging, metrics, tracing, health checks, service discovery)
AddTelemetry.ConfigureTelemetryAndServiceDefaults(builder);

// Configure logging
ConfigureServices.ConfigureLogging(builder);

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
ConfigureDatabase.AddDatabaseContexts(builder);

// Add Identity services
ConfigureIdentity.AddIdentityCore(builder);

// Host the API in-proc so endpoints are exposed by this same app
builder.Services.AddBudgetApi(builder.Configuration, builder.Environment);

var app = builder.Build();

// Initialize ServiceAccessor with built service provider for parameterless constructors
ServiceAccessor.Configure(app.Services);

// Apply database migrations and log startup info
var budgetConnectionString = Misc.GetConnectionString(builder.Configuration, Misc.ConnectionStringType.Budget);
ConfigureDatabase.ApplyMigrations(app, budgetConnectionString);

// Configure exception handling
ConfigureMiddleware.ConfigureExceptionHandling(app);

// Configure static files
ConfigureMiddleware.ConfigureStaticFiles(app);

// Configure middleware pipeline
ConfigureMiddleware.ConfigurePipeline(app);

// Map endpoints
ConfigureMiddleware.MapEndpoints(app);

app.Run();

