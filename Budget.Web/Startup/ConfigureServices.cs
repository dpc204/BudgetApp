using Budget.Client.Services;
using Budget.Web.Services;
using MudBlazor.Services;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

namespace Budget.Web.Startup;

/// <summary>
/// Configures application services including HTTP clients, UI libraries, and business services
/// </summary>
public static class ConfigureServices
{
  /// <summary>
  /// Adds Blazor components and rendering modes
  /// </summary>
  public static void AddBlazorServices(WebApplicationBuilder builder)
  {
    builder.Services.AddRazorComponents()
      .AddInteractiveServerComponents();

    builder.Services.AddCascadingAuthenticationState();
  }

  /// <summary>
  /// Configures HTTP clients for API communication with authentication forwarding
  /// </summary>
  public static void AddHttpClients(WebApplicationBuilder builder)
  {
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddTransient<ForwardAuthCookiesHandler>();

    // Use Aspire service discovery for Budget API
    // The service name "budget-api" matches the name defined in AppHost
    builder.Services.AddHttpClient<IBudgetApiClient, BudgetApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

    builder.Services.AddHttpClient<IBudgetMaintApiClient, BudgetMaintApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();
  }

  /// <summary>
  /// Adds UI component libraries (MudBlazor, Syncfusion)
  /// </summary>
  public static void AddUILibraries(WebApplicationBuilder builder)
  {
    // Register Syncfusion license
    SyncfusionLicenseProvider.RegisterLicense(
      "Ngo9BigBOggjGyl/Vkd+XU9FcVRDX3xflBPallYVBYiSV9jS3tTf0VkW35ecHFcRGdeUk91Xg==");

    builder.Services.AddSyncfusionBlazor();
    builder.Services.AddMudServices();
  }

  /// <summary>
  /// Adds application-specific services
  /// </summary>
  public static void AddApplicationServices(WebApplicationBuilder builder)
  {
    builder.Services.AddScoped<EnvelopeState>();
    builder.Services.AddSingleton<ThemeService>();
    builder.Services.AddScoped<IUserAndOptions, UserAndOptions>();
  }

  /// <summary>
  /// Configures custom logging filters
  /// </summary>
  public static void ConfigureLogging(WebApplicationBuilder builder)
  {
    builder.Logging.AddJsonConsole();
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Trace);
    builder.Logging.AddFilter("Budget.Client.Components.Maintenance.AccountCRUD", LogLevel.Debug);
  }

  /// <summary>
  /// Gets the API base address from configuration with fallbacks
  /// </summary>
  private static string GetApiBaseAddress(IConfiguration configuration)
  {
    string apiBase = configuration["BUDGET_API_BASE_URL"]
                     ?? configuration["ApiBaseUrl"]
                     ?? configuration["Api:BaseUrl"]
                     ?? configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault()
                     ?? "http://127.0.0.1:8080";

    return NormalizeBaseAddress(apiBase);
  }

  /// <summary>
  /// Normalizes wildcard binds (0.0.0.0 or +) to loopback so HttpClient can connect in-proc
  /// </summary>
  private static string NormalizeBaseAddress(string value)
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
}
