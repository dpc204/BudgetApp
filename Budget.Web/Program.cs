using Azure.Identity;
using Budget.DB;
using Budget.Web.Components;
using Budget.Web.Components.Account;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Syncfusion.Blazor;
using System.Diagnostics;
using Budget.Shared;
using Budget.Shared.Services;
using Budget.DTO; // interface & DTOs
using Budget.Client.Services; // implementation

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents()
  .AddInteractiveWebAssemblyComponents()
  .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var apiBase = builder.Configuration["BUDGET_API_BASE_URL"]
             ?? builder.Configuration["ApiBaseUrl"]
             ?? builder.Configuration["Api:BaseUrl"]
             ?? builder.Configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault()
             ?? "https://localhost:5001"; // final fallback

builder.Services.AddHttpClient<Budget.DTO.IBudgetApiClient, Budget.Client.Services.BudgetApiClient>(client =>
{
  if (!apiBase.EndsWith('/')) apiBase += "/";
  client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddScoped<EnvelopeState>();
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
  "Ngo9BigBOggjGyl/Vkd+XU9FcVRDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS3tTf0VkW35ecHFcRGdeUk91Xg==");

builder.Services.AddAuthorization(options => { options.AddPolicy("Admin", policy => policy.RequireRole("Admin")); });

builder.Services.AddAuthentication(options =>
  {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
  })
  .AddIdentityCookies();

var connectionString = Misc.SetupConfigurationSources(builder.Configuration, builder.Configuration, typeof(Program).Assembly);

builder.Services.AddDbContext<BudgetContext>((sp, options) =>
{
  var logger = sp.GetRequiredService<ILogger<BudgetContext>>();
  options.UseSqlServer(connectionString).EnableSensitiveDataLogging();
});

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
  var logger = sp.GetRequiredService<ILogger<ApplicationDbContext>>();
  options.UseSqlServer(connectionString).EnableSensitiveDataLogging();
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
  {
    options.SignIn.RequireConfirmedAccount = true;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
  })
  .AddEntityFrameworkStores<ApplicationDbContext>()
  .AddSignInManager()
  .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddSyncfusionBlazor();

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation(
  "Application starting at {UtcTime} with DB host parsed from connection string: {DataSource}", DateTime.UtcNow,
  ParseDataSource(connectionString));

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
  app.UseWebAssemblyDebugging();
  app.UseMigrationsEndPoint();
}
else
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseDeveloperExceptionPage();
app.MapStaticAssets();

app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode()
  .AddInteractiveWebAssemblyRenderMode()
  .AddAdditionalAssemblies(typeof(Budget.Client._Imports).Assembly);

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

