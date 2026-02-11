using Budget.Client.Services;
using Budget.Client.Services.ApiClients;
using Budget.Web.Services;
using MudBlazor.Services;
using Syncfusion.Blazor;
using Syncfusion.Licensing;
using System.Globalization;

namespace Budget.Web.Startup;

/// <summary>
/// Configures application services including HTTP clients, UI libraries, and business services
/// </summary>
public static class ConfigureServices
{
  /// <summary>
  /// Configures the default culture for the application to ensure consistent formatting
  /// </summary>
  public static void ConfigureGlobalization(WebApplicationBuilder builder)
  {
    // Set default culture to en-US for consistent currency formatting
    var defaultCulture = new CultureInfo("en-US");
    CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
    CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
      var supportedCultures = new[] { defaultCulture };
      options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture);
      options.SupportedCultures = supportedCultures;
      options.SupportedUICultures = supportedCultures;
    });
  }

  /// <summary>
  /// Adds Blazor components and rendering modes
  /// </summary>
  public static void AddBlazorServices(WebApplicationBuilder builder)
  {
    // Get timeout from configuration for SignalR circuits (default 30 seconds if not specified)
    var circuitTimeoutSeconds =
      builder.Configuration.GetValue<int>("CircuitOptions:DisconnectedCircuitRetentionPeriod", 180);
    var circuitRetentionPeriod = TimeSpan.FromSeconds(circuitTimeoutSeconds);

    builder.Services.AddRazorComponents()
      .AddInteractiveServerComponents();

    builder.Services.AddCascadingAuthenticationState();

    // Configure SignalR Hub options for longer-running operations
    builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
    {
      options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
      options.ClientTimeoutInterval = TimeSpan.FromMinutes(10); // Client must send message within this time
      options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });

    // Configure Blazor Server Circuit options
    builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
    {
      options.DisconnectedCircuitMaxRetained = 100;
      options.DisconnectedCircuitRetentionPeriod = circuitRetentionPeriod;
      options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
    });
  }

  /// <summary>
  /// Configures HTTP clients for API communication with authentication forwarding
  /// </summary>
  public static void AddHttpClients(WebApplicationBuilder builder)
  {
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<TokenCacheManager>();
    builder.Services.AddTransient<ForwardAuthCookiesHandler>();

    // Check if we're in development mode - skip resilience handlers for faster debugging
    var isDevelopment = builder.Environment.IsDevelopment();

    // Get timeout from configuration (default 100 seconds if not specified)
    var timeoutSeconds = builder.Configuration.GetValue<int>("HttpClient:TimeoutSeconds", 100);
    var timeout = TimeSpan.FromSeconds(timeoutSeconds);

    // Standard resilience options for normal operations
    void ConfigureStandardResilience(Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions options)
    {
      // Keep retries enabled for transient failures
      options.Retry.MaxRetryAttempts = 3;
      options.Retry.Delay = TimeSpan.FromSeconds(2);

      // Standard timeout (30 seconds per attempt, 100 seconds total)
      options.AttemptTimeout = new Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions
      {
        Timeout = TimeSpan.FromSeconds(30)
      };
      options.TotalRequestTimeout = new Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions
      {
        Timeout = TimeSpan.FromSeconds(100)
      };
      options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(200);
    }

    // Long-running resilience options (minimal retries, VERY long timeouts)
    void ConfigureLongRunningResilience(Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions options)
    {
      // Minimal retries for long-running operations
      options.Retry.MaxRetryAttempts = 1;
      options.Retry.Delay = TimeSpan.FromSeconds(1);

      // CRITICAL: Both AttemptTimeout and TotalRequestTimeout must match the HttpClient timeout
      options.AttemptTimeout = new Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions
      {
        Timeout = timeout // 5 minutes
      };
      options.TotalRequestTimeout = new Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions
      {
        Timeout = timeout // Same as AttemptTimeout since we're not retrying
      };

      // Circuit breaker sampling must be at least 2x the attempt timeout
      options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(timeoutSeconds * 2);
    }

    // Feature-aligned API clients using Aspire service discovery
#pragma warning disable EXTEXP0001

    // Legacy BudgetApiClient (used by EnvelopeState and other legacy components)
    var budgetApiClientBuilder = builder.Services.AddHttpClient<IBudgetApiClient, BudgetApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
        client.Timeout = timeout;
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

    if (!isDevelopment)
    {
      budgetApiClientBuilder.AddStandardResilienceHandler(ConfigureStandardResilience);
    }

    // Envelopes API Client
    var envelopesApiClientBuilder = builder.Services.AddHttpClient<IEnvelopesApiClient, EnvelopesApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
        client.Timeout = timeout;
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

    if (!isDevelopment)
    {
      envelopesApiClientBuilder.AddStandardResilienceHandler(ConfigureStandardResilience);
    }

    // Categories API Client
    var categoriesApiClientBuilder = builder.Services.AddHttpClient<ICategoriesApiClient, CategoriesApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
        client.Timeout = timeout;
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

    if (!isDevelopment)
    {
      categoriesApiClientBuilder.AddStandardResilienceHandler(ConfigureStandardResilience);
    }

    // Transactions API Client
    var transactionsApiClientBuilder = builder.Services.AddHttpClient<ITransactionsApiClient, TransactionsApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
        client.Timeout = timeout;
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

    if (!isDevelopment)
    {
      transactionsApiClientBuilder.AddStandardResilienceHandler(ConfigureStandardResilience);
    }

    // Accounts API Client
    var accountsApiClientBuilder = builder.Services.AddHttpClient<IAccountsApiClient, AccountsApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
        client.Timeout = timeout;
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

    if (!isDevelopment)
    {
      accountsApiClientBuilder.AddStandardResilienceHandler(ConfigureStandardResilience);
    }

    // Admin API Client
    var adminApiClientBuilder = builder.Services.AddHttpClient<IAdminApiClient, AdminApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
        client.Timeout = timeout;
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

    if (!isDevelopment)
    {
      adminApiClientBuilder.AddStandardResilienceHandler(ConfigureStandardResilience);
    }

    // UserOptions API Client
    var userOptionsApiClientBuilder = builder.Services.AddHttpClient<IUserOptionsApiClient, UserOptionsApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
        client.Timeout = timeout;
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

    if (!isDevelopment)
    {
      userOptionsApiClientBuilder.AddStandardResilienceHandler(ConfigureStandardResilience);
    }

    // Utilities API Client
    var utilitiesApiClientBuilder = builder.Services.AddHttpClient<IUtilitiesApiClient, UtilitiesApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
        client.Timeout = timeout;
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

    if (!isDevelopment)
    {
      utilitiesApiClientBuilder
        .RemoveAllResilienceHandlers()
        .AddStandardResilienceHandler(ConfigureLongRunningResilience);
    }

    // BudgetMonthly API Client
    var budgetMonthlyApiClientBuilder = builder.Services.AddHttpClient<IBudgetMonthlyApiClient, BudgetMonthlyApiClient>(client =>
      {
        client.BaseAddress = new Uri("https+http://budget-api");
        client.Timeout = timeout;
      })
      .AddHttpMessageHandler<ForwardAuthCookiesHandler>();

    if (!isDevelopment)
    {
      budgetMonthlyApiClientBuilder
        .RemoveAllResilienceHandlers()
        .AddStandardResilienceHandler(ConfigureLongRunningResilience);
    }

#pragma warning restore EXTEXP0001
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
    // Token cache persistence strategy:
    // - Always use SQL Server distributed cache (works both locally and in Azure)
    // - Fallback to in-memory if SQL Server is not available

    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

    // Get connection string from the registered provider
    var serviceProvider = builder.Services.BuildServiceProvider();
    var connectionStringProvider = serviceProvider.GetService<IConnectionStringProvider>();
    var sqlConnection = connectionStringProvider?.BudgetConnectionString
                        ?? builder.Configuration["LocalBudgetConnection"]
                        ?? builder.Configuration["BudgetConnection"];

    if (!string.IsNullOrEmpty(sqlConnection))
    {
      logger.LogInformation("Configuring SQL Server distributed cache for token persistence");
      logger.LogInformation("Connection string source: {Source}",
        connectionStringProvider != null ? "ConnectionStringProvider" : "Configuration");
      logger.LogInformation("Connection string (first 50 chars): {ConnString}",
        sqlConnection[..Math.Min(50, sqlConnection.Length)]);

      builder.Services.AddDistributedSqlServerCache(options =>
      {
        options.ConnectionString = sqlConnection;
        options.SchemaName = "dbo";
        options.TableName = "SessionCache";
        logger.LogInformation("SQL Distributed Cache configured: Schema={Schema}, Table={Table}", options.SchemaName,
          options.TableName);
      });
    }
    else
    {
      // Fallback to in-memory (development only - tokens won't persist)
      logger.LogWarning(
        "NO SQL CONNECTION STRING FOUND! Using in-memory cache (tokens will NOT persist across restarts)");
      builder.Services.AddDistributedMemoryCache();
    }

    builder.Services.AddScoped<EnvelopeState>();
    builder.Services.AddSingleton<ThemeService>();

    // Register data provider for frontend (uses API client)
    builder.Services.AddScoped<IUserAndOptionsDataProvider, ApiUserAndOptionsDataProvider>();
    builder.Services.AddScoped<IUserAndOptions, UserAndOptions>();

    // Register role management service
    builder.Services.AddScoped<IRoleService, RoleService>();

    // Register Fund page services
    builder.Services.AddScoped<IFundAllocationService, FundAllocationService>();
    builder.Services.AddScoped<IFundDataService, FundDataService>();

    // Register Envelope page services
    builder.Services.AddScoped<IEnvelopeDataService, EnvelopeDataService>();
    builder.Services.AddScoped<IEnvelopeTransactionService, EnvelopeTransactionService>();

    // Do not register IBudgetMonthlyApiClient again here - configured by AddHttpClient
  }

  /// <summary>
  /// Configures custom logging filters
  /// </summary>
  public static void ConfigureLogging(WebApplicationBuilder builder)
  {
    builder.Logging.AddJsonConsole();
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Debug);

    // Suppress verbose MSAL (Microsoft Authentication Library) logging
    // MSAL outputs detailed trace logs for every token acquisition attempt
    builder.Logging.AddFilter("Microsoft.Identity.Client", LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.Identity.Web", LogLevel.Warning);
  }

  /// <summary>
  /// Configures Kestrel server limits
  /// </summary>
  public static void ConfigureKestrel(WebApplicationBuilder builder)
  {
    var requestHeadersTimeoutMinutes =
      builder.Configuration.GetValue<int>("Kestrel:Limits:RequestHeadersTimeoutMinutes", 5);
    var keepAliveTimeoutMinutes = builder.Configuration.GetValue<int>("Kestrel:Limits:KeepAliveTimeoutMinutes", 5);

    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
      serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(requestHeadersTimeoutMinutes);
      serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(keepAliveTimeoutMinutes);
    });
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
      if (value.Contains("0.0.0.0", StringComparison.Ordinal) || value.Contains('+', StringComparison.Ordinal))
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
}