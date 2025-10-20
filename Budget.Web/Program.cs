using Azure.Identity;
using Budget.DB;
using Budget.Shared;
using Budget.Shared.Models;
using Budget.Shared.Services;
using Budget.Web.Components;
using Budget.Web.Components.Account;
using Budget.Web.Data;
using Budget.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Syncfusion.Blazor;
using Budget.Api; // host API in-proc

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Ensure EF Core command logs go through ILogger and to structured logs
builder.Logging.AddJsonConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);

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

builder.Services.AddHttpClient<IBudgetApiClient, Budget.Client.Services.BudgetApiClient>(client =>
{
  if(!apiBase.EndsWith('/'))
    apiBase += "/";
  client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddHttpClient<IBudgetMaintApiClient, Budget.Client.Services.BudgetMaintApiClient>(client =>
{
  if(!apiBase.EndsWith('/'))
    apiBase += "/";
  client.BaseAddress = new Uri(apiBase);
});

// Host the API in-proc so endpoints are exposed by this same app
builder.Services.AddBudgetApi(builder.Configuration, builder.Environment);

builder.Services.AddScoped<EnvelopeState>();

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
  "Ngo9BigBOggjGyl/Vkd+XU9FcVRDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS3tTf0VkW35ecHFcRGdeUk91Xg==");

builder.Services.AddAuthorization(options => { options.AddPolicy("Admin", policy => policy.RequireRole("Admin")); });

builder.Services.AddAuthentication(options =>
  {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
  })
  .AddIdentityCookies(options =>
  {
    // Prevent automatic redirects for Blazor Server - let components handle auth
    options.ApplicationCookie.Configure(cookieOptions =>
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

var budgetConnectionString = Misc.SetupConfigurationSources(builder.Configuration, builder.Configuration, typeof(Program).Assembly, Misc.ConnectionStringType.Budget);
var authConnectionString = Misc.SetupConfigurationSources(builder.Configuration, builder.Configuration, typeof(Program).Assembly, Misc.ConnectionStringType.Identity);

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

builder.Services.AddDbContext<IdentityDBContext>(options =>
  options.UseSqlServer(authConnectionString));

Console.WriteLine($"Right after Add IdentityDBContext {authConnectionString}");

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
var app = builder.Build();

// Initialize ServiceAccessor with built service provider for parameterless constructors
ServiceAccessor.Configure(app.Services);

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation(
  "Application starting at {UtcTime} with BudgetDB host parsed from connection string: {DataSource}", DateTime.UtcNow,
  ParseDataSource(budgetConnectionString));

startupLogger.LogInformation(
  "Application starting at {UtcTime} with IdentityDB host parsed from connection string: {DataSource}", DateTime.UtcNow,
  ParseDataSource(authConnectionString));

app.MapDefaultEndpoints();

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
  .AddAdditionalAssemblies(typeof(Budget.Client.Pages.Home).Assembly); // enable routes from Budget.Client RCL

app.MapAdditionalIdentityEndpoints();

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
  catch
  {
    return "http://127.0.0.1:8080";
  }
}

