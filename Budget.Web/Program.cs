using System.Diagnostics;
using Microsoft.SqlServer.Dac;
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

// Ensure EF Core command logs go through ILogger and to structured logs
builder.Logging.AddJsonConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Trace);

builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddBlazorBootstrap();
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

var budgetConnectionString = Misc.SetupConfigurationSources(builder.Configuration, builder.Configuration,
  typeof(Program).Assembly, Misc.ConnectionStringType.Budget);
var authConnectionString = Misc.SetupConfigurationSources(builder.Configuration, builder.Configuration,
  typeof(Program).Assembly, Misc.ConnectionStringType.Identity);

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

// Host the API in-proc so endpoints are exposed by this same app (moved here, after Identity)
builder.Services.AddBudgetApi(builder.Configuration, builder.Environment);

// Register backup service
builder.Services.AddHttpClient<BackupAzureSql>();

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

// Plan endpoint: compute the filename that will be used for the next backup
app.MapGet("/api/maintenance/backup-plan", (IServiceProvider sp) =>
{
 using var scope = sp.CreateScope();
 var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
 var conn = db.Database.GetDbConnection();
 var databaseName = conn.Database;
 var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
 var fileName = $"{databaseName}-{stamp}.bacpac";
 return Results.Ok(new { fileName });
}).RequireAuthorization("Admin");

// Simple local export: stream a bacpac generated by DacFx (no external tools)
app.MapGet("/api/maintenance/backup-download", async (HttpContext http, IServiceProvider sp, ILoggerFactory lf, CancellationToken ct) =>
{
 var log = lf.CreateLogger("MaintenanceBackupDownload");
 await using var scope = sp.CreateAsyncScope();
 var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
 var conn = db.Database.GetDbConnection();
 var connString = conn.ConnectionString;
 var databaseName = conn.Database;

 // Optional name from query so client can pre-display the exact filename
 var requestedName = http.Request.Query["name"].ToString();
 var fileName = !string.IsNullOrWhiteSpace(requestedName)
 ? requestedName
 : $"{databaseName}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.bacpac";

 var tempPath = Path.Combine(Path.GetTempPath(), fileName);

 try
 {
 var dac = new DacServices(connString);
 log.LogInformation("Starting DacFx export of {Database} to {File}", databaseName, tempPath);
 await Task.Run(() => dac.ExportBacpac(tempPath, databaseName), ct);

 if (!System.IO.File.Exists(tempPath))
 {
 log.LogError("DacFx reported success but file not found: {File}", tempPath);
 return Results.Problem("Export failed: output file missing.", statusCode:500);
 }

 log.LogInformation("Export complete. Streaming {FileName} ({Size} bytes)", fileName, new FileInfo(tempPath).Length);
 var stream = System.IO.File.OpenRead(tempPath);
 return Results.File(stream, "application/octet-stream", fileName, enableRangeProcessing: false);
 }
 catch (Exception ex)
 {
 log.LogError(ex, "Error running DacFx export");
 return Results.Problem(ex.ToString(), statusCode:500);
 }
 finally
 {
 _ = Task.Run(async () =>
 {
 try
 {
 await Task.Delay(TimeSpan.FromMinutes(5));
 if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath);
 }
 catch { }
 });
 }
}).RequireAuthorization("Admin");

// Maintenance API: trigger Azure SQL export
app.MapPost("/api/maintenance/backup-azure-sql",
  async (BackupAzureSql backup, IConfiguration cfg, ILoggerFactory lf, CancellationToken ct) =>
  {
    var log = lf.CreateLogger("MaintenanceBackup");
    try
    {
      var subscriptionId = cfg["AzureSqlSubscriptionId"] ?? string.Empty;
      var resourceGroup = cfg["AzureSqlResourceGroup"] ?? string.Empty;
      var serverName = cfg["AzureSqlServerName"] ?? string.Empty;
      var databaseName = cfg["AzureSqlDatabaseName"] ?? string.Empty;
      var storageKey = cfg["AzureSqlStorageKey"] ?? string.Empty;
      var storageUri = cfg["AzureSqlStorageUri"] ?? string.Empty; // full blob URI to .bacpac or container URL
      var dbAdmin = cfg["AzureSqlDbAdmin"] ?? string.Empty;
      var dbPassword = cfg["AzureSqlDbPassword"] ?? string.Empty;

      storageKey =
        "se=2025-11-01T01%3A08Z&sp=acwl&sv=2022-11-02&sr=c&sig=6QWi4aVIVXpPbN4JVFSCSlaq0pPZHglKu9i9NSk19X8%3D";

      var missing = new List<string>();
      if (string.IsNullOrWhiteSpace(subscriptionId)) missing.Add("AzureSqlSubscriptionId");
      if (string.IsNullOrWhiteSpace(resourceGroup)) missing.Add("AzureSqlResourceGroup");
      if (string.IsNullOrWhiteSpace(serverName)) missing.Add("AzureSqlServerName");
      if (string.IsNullOrWhiteSpace(databaseName)) missing.Add("AzureSqlDatabaseName");
      if (string.IsNullOrWhiteSpace(storageKey)) missing.Add("AzureSqlStorageKey");
      if (string.IsNullOrWhiteSpace(storageUri)) missing.Add("AzureSqlStorageUri");
      if (string.IsNullOrWhiteSpace(dbAdmin)) missing.Add("AzureSqlDbAdmin");
      if (string.IsNullOrWhiteSpace(dbPassword)) missing.Add("AzureSqlDbPassword");
      if (missing.Count > 0)
      {
        var payload = new { error = "Missing AzureSql configuration values.", missing };
        log.LogWarning("Backup request rejected due to missing configuration: {Missing}", string.Join(", ", missing));
        return Results.BadRequest(payload);
      }

      // If StorageUri points to a container (or just the account root), append a guaranteed-unique filename
      if (!storageUri.EndsWith(".bacpac", StringComparison.OrdinalIgnoreCase))
      {
        // Normalize base and ensure a container segment
        if (!Uri.TryCreate(storageUri, UriKind.Absolute, out var su))
        {
          return Results.BadRequest(new { error = "AzureSqlStorageUri is not a valid absolute URI.", storageUri });
        }

        var baseUrl = $"{su.Scheme}://{su.Host}";
        var path = su.AbsolutePath?.Trim('/') ?? string.Empty; // may be empty for account root
        if (string.IsNullOrWhiteSpace(path))
        {
          path = "sqlserver-backups"; // default container if none provided
          log.LogInformation("No container specified in StorageUri. Using default container '{Container}'.", path);
        }

        var sep = path.EndsWith('/') ? string.Empty : "/";
        var uniqueName = $"{databaseName}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.bacpac";
        storageUri = $"{baseUrl}/{path}{sep}{uniqueName}";
        log.LogInformation("Computed export blob path: {Blob}", storageUri);
      }

      var result = await backup.ExportDatabaseAsync(subscriptionId, resourceGroup, serverName, databaseName, storageKey,
        storageUri, dbAdmin, dbPassword, ct);
      return Results.Ok(result);
    }
    catch (Exception ex)
    {
      var log2 = lf.CreateLogger("MaintenanceBackup");
      log2.LogError(ex, "Backup failed");
      return Results.Problem(ex.ToString(), statusCode: 500);
    }
  }).RequireAuthorization("Admin");

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