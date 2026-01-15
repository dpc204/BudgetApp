using Budget.Web.Authorization;
using Budget.Web.Components.Account;
using Budget.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Client;

namespace Budget.Web.Startup;

/// <summary>
/// Configures Microsoft Entra ID authentication and authorization services
/// </summary>
public static class ConfigureIdentity
{
  /// <summary>
  /// Adds authentication with Microsoft Entra ID (OpenID Connect)
  /// </summary>
  public static void AddAuthentication(WebApplicationBuilder builder)
  {
    // DIAGNOSTIC: Log all AzureAd configuration values to verify ClientSecret is loaded
    var azureAdSection = builder.Configuration.GetSection("AzureAd");
    var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information));
    var logger = loggerFactory.CreateLogger("ConfigureIdentity");
    
    logger.LogInformation("=== AzureAd Configuration at Authentication Setup ===");
    logger.LogInformation("Instance: {Instance}", azureAdSection["Instance"]);
    logger.LogInformation("Domain: {Domain}", azureAdSection["Domain"]);
    logger.LogInformation("TenantId: {TenantId}", azureAdSection["TenantId"]);
    logger.LogInformation("ClientId: {ClientId}", azureAdSection["ClientId"]);
    logger.LogInformation("ClientSecret present: {HasSecret}", !string.IsNullOrEmpty(azureAdSection["ClientSecret"]));
    if (!string.IsNullOrEmpty(azureAdSection["ClientSecret"]))
    {
      logger.LogInformation("ClientSecret length: {Length}", azureAdSection["ClientSecret"]?.Length ?? 0);
    }
    else
    {
      logger.LogError("ClientSecret is NULL or EMPTY! This will cause confidential client creation to fail.");
      logger.LogInformation("All AzureAd keys in configuration: {Keys}", string.Join(", ", azureAdSection.AsEnumerable().Select(kvp => kvp.Key)));
    }
    logger.LogInformation("CallbackPath: {CallbackPath}", azureAdSection["CallbackPath"]);
    logger.LogInformation("SignedOutCallbackPath: {SignedOutCallbackPath}", azureAdSection["SignedOutCallbackPath"]);
    
    // Verify distributed cache is registered
    var cacheRegistration = builder.Services.FirstOrDefault(s => s.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
    if (cacheRegistration != null)
    {
      logger.LogInformation("Distributed cache is registered: {Implementation}", cacheRegistration.ImplementationType?.Name ?? "Unknown");
    }
    else
    {
      logger.LogWarning("NO DISTRIBUTED CACHE FOUND! Tokens will not persist!");
    }
    
    // Configure Microsoft Entra ID authentication with token acquisition
    // Budget.Api accepts Entra ID JWT tokens, so we need to acquire access tokens
    // Tokens are cached in SQL Server distributed cache for persistence across app restarts
    
    // Get the API scope before configuring authentication
    var apiClientId = azureAdSection["ClientId"];
    var apiScope = !string.IsNullOrEmpty(apiClientId) ? $"api://{apiClientId}/access_as_user" : "";
    
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
      .AddMicrosoftIdentityWebApp(options =>
      {
        azureAdSection.Bind(options);
        
        // Configure OpenIdConnect events to ensure proper token caching
        options.Events ??= new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents();
        
        var existingOnTokenValidated = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async context =>
        {
          // Call existing handler if any
          if (existingOnTokenValidated != null)
          {
            await existingOnTokenValidated(context);
          }
          
          // Clear any failed session state from TokenCacheValidationMiddleware
          // This ensures a fresh start after successful re-authentication
          var sessionId = context.Principal?.FindFirst("sid")?.Value ?? 
                          context.Principal?.FindFirst("oid")?.Value ??
                          context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
          
          if (!string.IsNullOrEmpty(sessionId))
          {
            Budget.Web.Middleware.TokenCacheValidationMiddleware.ClearSessionValidation(sessionId);
            logger.LogInformation("Cleared session validation state for {SessionId} after successful token validation", sessionId);
          }
          
          logger.LogInformation("? Token validated for user {User}. Tokens will be cached by Microsoft.Identity.Web.", 
            context.Principal?.Identity?.Name ?? "unknown");
        };
        
        var existingOnAuthorizationCodeReceived = options.Events.OnAuthorizationCodeReceived;
        options.Events.OnAuthorizationCodeReceived = async context =>
        {
          // Call existing handler if any
          if (existingOnAuthorizationCodeReceived != null)
          {
            await existingOnAuthorizationCodeReceived(context);
          }
          
          logger.LogInformation("? Authorization code received - Microsoft.Identity.Web will exchange for tokens and cache them");
        };
        
        var existingOnTokenResponseReceived = options.Events.OnTokenResponseReceived;
        options.Events.OnTokenResponseReceived = async context =>
        {
          // Call existing handler if any
          if (existingOnTokenResponseReceived != null)
          {
            await existingOnTokenResponseReceived(context);
          }
          
          logger.LogInformation("? Token response received - tokens are now cached in distributed cache");
        };
      })
      .EnableTokenAcquisitionToCallDownstreamApi(
        [apiScope] // Automatically acquires and caches API token during sign-in
      )
      .AddDistributedTokenCaches(); // Uses SQL Server distributed cache configured in ConfigureServices
    
    logger.LogInformation("? Configured authentication with automatic token acquisition for scope: {ApiScope}", apiScope);
    logger.LogInformation("Default authentication scheme: {Scheme}", OpenIdConnectDefaults.AuthenticationScheme);
    logger.LogInformation("Cookie authentication scheme: {CookieScheme}", CookieAuthenticationDefaults.AuthenticationScheme);
    
    logger.LogInformation("Token caching configured with distributed cache");
    
    if (!string.IsNullOrEmpty(apiScope))
    {
      logger.LogInformation("Configured API scope: {ApiScope}", apiScope);
    }
    else
    {
      logger.LogWarning("API scope not configured - token caching may not work properly");
    }
    
    logger.LogInformation("Microsoft Entra ID authentication with token acquisition configured successfully");
    loggerFactory.Dispose();


    // Configure cookie authentication options for the scheme used by Microsoft.Identity.Web
    // Microsoft.Identity.Web uses CookieAuthenticationDefaults.AuthenticationScheme for cookies
    builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
      options.Cookie.HttpOnly = true;
      options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
      options.Cookie.SameSite = SameSiteMode.Lax;
      options.ExpireTimeSpan = TimeSpan.FromHours(8); // Reasonable session length
      options.SlidingExpiration = true; // Extend expiration on each request
      options.Cookie.Name = "Budget.Auth"; // Custom cookie name
      options.LoginPath = "/MicrosoftIdentity/Account/SignIn"; // Redirect here if not authenticated
      options.LogoutPath = "/MicrosoftIdentity/Account/SignOut";
      options.AccessDeniedPath = "/Account/AccessDenied";
      
      // NOTE: OnValidatePrincipal was causing infinite redirect loops because:
      // 1. Token acquisition requires account info in the principal
      // 2. The principal from the cookie doesn't have enough info for MSAL to find the cached token
      // 3. This causes "user_null" error every time, rejecting the principal
      // 4. Which causes a redirect loop
      //
      // Solution: Let the token expire naturally, and handle refresh in ForwardAuthCookiesHandler
      // Microsoft.Identity.Web automatically handles token refresh during API calls
      
      logger.LogInformation("Cookie authentication options configured (no aggressive validation)");
    });

    // Add controllers with views for Microsoft Identity UI
    builder.Services.AddControllersWithViews()
      .AddMicrosoftIdentityUI();
  }

  /// <summary>
  /// Adds authorization policies for role-based access control using database roles
  /// </summary>
  public static void AddAuthorization(WebApplicationBuilder builder)
  {
    // Register claims transformation to load database roles after Entra ID authentication
    builder.Services.AddScoped<IClaimsTransformation, DatabaseClaimsTransformation>();
    
    // Register authorization handler for database roles
    builder.Services.AddSingleton<IAuthorizationHandler, DatabaseRoleHandler>();
    
    // Configure authorization policies using database roles
    builder.Services.AddAuthorizationBuilder()
      .AddPolicy("Admin", policy =>
        policy.Requirements.Add(new DatabaseRoleRequirement("Admin")))
      .AddPolicy("AdminOnly", policy =>
        policy.Requirements.Add(new DatabaseRoleRequirement("Admin")))
      .AddPolicy("PowerUser", policy =>
        policy.Requirements.Add(new DatabaseRoleRequirement("PowerUser")))
      .AddPolicy("PowerUserOrAbove", policy =>
        policy.Requirements.Add(new DatabaseRoleRequirement("Admin", "PowerUser")))
      .AddPolicy("User", policy =>
        policy.Requirements.Add(new DatabaseRoleRequirement("User")));
  }

  /// <summary>
  /// Configures ASP.NET Core Identity services
  /// NOTE: This is temporarily disabled during migration to Entra ID (Phase 2).
  /// Identity database and services will be removed in Phase 4 after data migration is complete.
  /// </summary>
  public static void AddIdentityCore(WebApplicationBuilder builder)
  {
    // TEMPORARY: Identity Core services are being phased out in favor of Entra ID
    // These services are kept for backward compatibility during the migration phase
    // They will be completely removed in Phase 4 after all users are migrated to Entra ID
    
    // Scoped services for Identity (still needed by some components during migration)
    builder.Services.AddScoped<IdentityUserAccessor>();
    builder.Services.AddScoped<IdentityRedirectManager>();
    
    // Replace the default authentication state provider with Entra-aware version
    builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

    // COMMENTED OUT: Identity Core configuration (will be removed in Phase 4)
    /*
    builder.Services.AddIdentityCore<BudgetUser>(options =>
      {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.ProtectPersonalData = false;
      })
      .AddRoles<IdentityRole>()
      .AddEntityFrameworkStores<Data.IdentityDBContext>()
      .AddSignInManager()
      .AddDefaultTokenProviders();

    builder.Services.AddSingleton<IEmailSender<BudgetUser>, IdentityNoOpEmailSender>();
    */
  }
}
