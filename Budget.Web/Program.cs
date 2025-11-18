using Azure.Identity;
using Budget.Api;
using Budget.DB;
using Budget.Shared;
using Budget.Shared.Models;
using Budget.Shared.Services;
using Budget.Web.Components;
using Budget.Web.Components.Account;
using Budget.Web.Data;
using Budget.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.SqlServer.Dac;
using MudBlazor.Services;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Syncfusion.Blazor;
using System.Diagnostics;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);
var assembly = typeof(App).Assembly;
Misc.SetupConfigurationSources(builder, assembly);

// Manual Aspire defaults (structured logs, traces, metrics, health, discovery)
ConfigureTelemetryAndServiceDefaults(builder);

// Ensure EF Core command logs go through ILogger and to structured logs
builder.Logging.AddJsonConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Trace);

builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
string apiBase = builder.Configuration["BUDGET_API_BASE_URL"]
                 ?? builder.Configuration["ApiBaseUrl"]
                 ?? builder.Configuration["Api:BaseUrl"]
                 ?? builder.Configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault()
                 ?? "http://127.0.0.1:8080"; // final fallback to loopback inside container


// Normalize wildcard binds (0.0.0.0 or +) to loopback so HttpClient can connect in-proc
apiBase = NormalizeBaseAddress(apiBase);

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<ForwardAuthCookiesHandler>();

builder.Services.AddHttpClient<IBudgetApiClient, Budget.Client.Services.BudgetApiClient>(client =>
  {
    if (!apiBase.EndsWith('/'))
      apiBase += "/";
    client.BaseAddress = new Uri(apiBase);
  })
  .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

builder.Services.AddHttpClient<IBudgetMaintApiClient, Budget.Client.Services.BudgetMaintApiClient>(client =>
  {
    if (!apiBase.EndsWith('/'))
      apiBase += "/";
    client.BaseAddress = new Uri(apiBase);
  })
  .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

builder.Services.AddScoped<EnvelopeState>();

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
  "Ngo9BigBOggjGyl/Vkd+XU9FcVRDX3xflBPallYVBYiSV9jS3tTf0VkW35ecHFcRGdeUk91Xg==");

builder.Services.AddAuthorization(options => { options.AddPolicy("Admin", policy => policy.RequireRole("Admin")); });

builder.Services.AddAuthentication(options =>
  {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
  })
  .AddIdentityCookies(options =>
  {
    options.ApplicationCookie?.Configure(cookieOptions =>
    {
      cookieOptions.Events.OnRedirectToLogin = context =>
      {
        // Don't redirect, just return 401
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
      };

      cookieOptions.Events.OnRedirectToAccessDenied = context =>
      {
        // Don't redirect, just return 403
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
      };
    });
  });

var budgetConnectionString = Misc.GetConnectionString(builder.Configuration, Misc.ConnectionStringType.Budget);
var authConnectionString = budgetConnectionString;

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

builder.Logging.AddFilter("Budget.Client.Components.Maintenance.AccountCRUD", LogLevel.Debug);

// Use the SAME database for Identity as BudgetContext (Identity schema within the same DB)
builder.Services.AddDbContext<IdentityDBContext>(options =>
  options.UseSqlServer(budgetConnectionString,
    o => o.MigrationsHistoryTable("__EFMigrationsHistory", "BudgetIdentity")));


builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSyncfusionBlazor();

builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddIdentityCore<BudgetUser>(options =>
  {
    options.SignIn.RequireConfirmedAccount = false;
    options.Stores.ProtectPersonalData = false;
  })
  .AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<IdentityDBContext>()
  .AddSignInManager()
  .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<BudgetUser>, IdentityNoOpEmailSender>();
builder.Services.AddMudServices();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddScoped<IUserAndOptions, UserAndOptions>();

// Host the API in-proc so endpoints are exposed by this same app (moved here, after Identity)
builder.Services.AddBudgetApi(builder.Configuration, builder.Environment);

var app = builder.Build();

// Initialize ServiceAccessor with built service provider for parameterless constructors
ServiceAccessor.Configure(app.Services);

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation(
  "Application starting at {UtcTime} with BudgetDB host parsed from connection string: {DataSource}", DateTime.UtcNow,
  ParseDataSource(budgetConnectionString));

startupLogger.LogInformation(
  "Application starting at {UtcTime} with IdentityDB host parsed from connection string: {DataSource}", DateTime.UtcNow,
  ParseDataSource(budgetConnectionString));

// Ensure Identity schema is created/migrated so 'BudgetIdentity.AspNetUsers' exists
using (var scope = app.Services.CreateScope())
{
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

if (app.Environment.IsDevelopment())
{
  app.UseMigrationsEndPoint();
  // Disable CSS Hot Reload to avoid Edge CSS rule limit issues
  app.UseStaticFiles(new StaticFileOptions
  {
    OnPrepareResponse = ctx =>
    {
      if (ctx.File.Name.EndsWith(".css"))
      {
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
      }
    }
  });
}
else
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  app.UseHsts();
  app.UseMigrationsEndPoint();
  app.UseStaticFiles();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}

app.MapStaticAssets();

// Map API endpoints in-proc
app.MapBudgetApi(app.Environment);

app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode()
  .AddAdditionalAssemblies(typeof(Budget.Client.Pages.Home).Assembly);

app.MapAdditionalIdentityEndpoints();

// Health endpoints (development only for security)
if (app.Environment.IsDevelopment())
{
  app.MapHealthChecks("/health");
  app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
}

app.Run();

static string? ParseDataSource(string cs)
{
  if (string.IsNullOrEmpty(cs)) return null;
  foreach (var part in cs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
  {
    if (part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
        part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
    {
      var idx = part.IndexOf('=');
      if (idx > -1 && idx < part.Length - 1)
        return part[(idx + 1)..];
    }
  }
  return null;
}

static string NormalizeBaseAddress(string value)
{
  try
  {
    if (string.IsNullOrWhiteSpace(value)) return "http://127.0.0.1:8080";
    if (value.Contains("0.0.0.0", StringComparison.Ordinal) || value.Contains("+", StringComparison.Ordinal))
    {
      var uri = new Uri(value);
      var port = uri.IsDefaultPort ? 80 : uri.Port;
      var scheme = string.IsNullOrEmpty(uri.Scheme) ? "http" : uri.Scheme;
      return $"{scheme}://127.0.0.1:{port}";
    }
    return value;
  }
  catch { return "http://127.0.0.1:8080"; }
}

static void ConfigureTelemetryAndServiceDefaults(WebApplicationBuilder builder)
{
  builder.Logging.AddOpenTelemetry(logging =>
  {
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
  });

  builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
      metrics.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation();
    })
    .WithTracing(tracing =>
    {
      tracing.AddSource(builder.Environment.ApplicationName)
        .AddAspNetCoreInstrumentation(o =>
        {
          o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health") && !ctx.Request.Path.StartsWithSegments("/alive");
        })
        .AddHttpClientInstrumentation();
    });

  var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]; // Aspire sets this when dashboard collects
  if(!string.IsNullOrWhiteSpace(otlpEndpoint))
  {
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
  }

  builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });
  builder.Services.AddServiceDiscovery();
  builder.Services.ConfigureHttpClientDefaults(http =>
  {
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
  });
}
