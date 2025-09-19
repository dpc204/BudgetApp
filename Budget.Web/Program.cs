using Budget.DB;
using Budget.Web.Components;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Blazor;
using Budget.Shared;
using Budget.Shared.Services;
using Budget.Web.Components.Account;
using Budget.Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Ensure EF Core command logs go through ILogger and to structured logs
builder.Logging.AddJsonConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);

builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents()
  .AddInteractiveWebAssemblyComponents()
  .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();

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
    options.SignIn.RequireConfirmedAccount = true;
    // Add this to disable passkey features
    options.Stores.ProtectPersonalData = false;
})
.AddEntityFrameworkStores<IdentityDBContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<BudgetUser>, IdentityNoOpEmailSender>();


var app = builder.Build();

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
app.UseStaticFiles();
app.MapStaticAssets();

app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode()
  .AddInteractiveWebAssemblyRenderMode()
  .AddAdditionalAssemblies(typeof(Budget.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();;


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

