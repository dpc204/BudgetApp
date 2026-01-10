using Budget.Web.Components.Account;
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
    
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
      .AddMicrosoftIdentityWebApp(options =>
      {
        azureAdSection.Bind(options);
        
        // Request API scope during sign-in so tokens are cached
        if (!string.IsNullOrEmpty(apiScope))
        {
          options.Scope.Clear();
          options.Scope.Add("openid");
          options.Scope.Add("profile");
          options.Scope.Add("offline_access"); // Enables refresh tokens
          options.Scope.Add(apiScope);
          logger.LogInformation("Requesting API scope during sign-in: {ApiScope}", apiScope);
        }
        
        // Force token acquisition after successful authentication
        options.Events.OnTokenValidated = async context =>
        {
          if (string.IsNullOrEmpty(apiScope))
            return;
            
          logger.LogInformation("Token validated - attempting to acquire API token for scope: {ApiScope}", apiScope);
          
          try
          {
            var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();
            
            // Proactively acquire and cache the API token
            var token = await tokenAcquisition.GetAccessTokenForUserAsync(
              new[] { apiScope },
              authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);
            
            if (!string.IsNullOrEmpty(token))
            {
              logger.LogInformation("? Successfully acquired and cached API token during sign-in (length: {Length})", token.Length);
            }
            else
            {
              logger.LogWarning("? Failed to acquire API token during sign-in - token was null");
            }
          }
          catch (Exception ex)
          {
            logger.LogError(ex, "? Error acquiring API token during sign-in: {Message}", ex.Message);
            // Don't fail the sign-in, but log the issue
          }
        };
        
        // Handle the case where we have a cookie but no MSAL account (after cache clear)
        options.Events.OnRedirectToIdentityProvider = context =>
        {
          // If we're trying to acquire a token but have no account, force re-authentication
          if (context.Properties.Items.ContainsKey(".AuthScheme") && 
              context.Properties.Items[".AuthScheme"] == OpenIdConnectDefaults.AuthenticationScheme)
          {
            logger.LogInformation("Redirect to identity provider - ensuring fresh authentication");
          }
          return Task.CompletedTask;
        };
      })
      .EnableTokenAcquisitionToCallDownstreamApi(options =>
      {
        // Configure default scopes for token acquisition
        if (!string.IsNullOrEmpty(apiScope))
        {
          logger.LogInformation("Configuring default scope for token acquisition: {ApiScope}", apiScope);
        }
      })
      .AddDistributedTokenCaches(); // Uses SQL Server distributed cache configured in ConfigureServices
    
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

    // Configure cookie authentication options for Blazor Server
    builder.Services.ConfigureApplicationCookie(options =>
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
      
      // Microsoft's recommended approach: Validate principal and refresh tokens if needed
      // This is the proper way to handle token expiration and cache misses
      options.Events.OnValidatePrincipal = async context =>
      {
        var tokenAcquisition = context.HttpContext.RequestServices.GetService<ITokenAcquisition>();
        var apiClientId = context.HttpContext.RequestServices.GetService<IConfiguration>()?["AzureAd:ClientId"];
        
        if (tokenAcquisition != null && !string.IsNullOrEmpty(apiClientId))
        {
          var apiScope = $"api://{apiClientId}/access_as_user";
          
          try
          {
            // Try to get a token silently - MSAL will handle refresh automatically
            // If the user has a valid session with Microsoft Entra ID, this will succeed
            // even if the local cache was cleared
            var token = await tokenAcquisition.GetAccessTokenForUserAsync(
              new[] { apiScope },
              authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);
              
            if (string.IsNullOrEmpty(token))
            {
              // Token acquisition failed - reject the principal to trigger re-auth
              logger.LogDebug("Cookie validation: No token available, rejecting principal");
              context.RejectPrincipal();
            }
            // Token acquired successfully - cookie is valid, continue
          }
          catch (MicrosoftIdentityWebChallengeUserException ex)
          {
            // User needs to re-authenticate (consent required, MFA, etc.)
            logger.LogDebug(ex, "Cookie validation: User challenge required, rejecting principal");
            context.RejectPrincipal();
          }
          catch (MsalUiRequiredException ex)
          {
            // User needs UI interaction (session expired at IdP)
            logger.LogDebug(ex, "Cookie validation: MSAL UI required, rejecting principal");
            context.RejectPrincipal();
          }
          catch (Exception ex)
          {
            // Log unexpected errors but don't reject - let the API call fail naturally
            // This prevents false positives from transient errors
            logger.LogDebug(ex, "Cookie validation: Token check failed with unexpected error");
          }
        }
      };
    });

    // Add controllers with views for Microsoft Identity UI
    builder.Services.AddControllersWithViews()
      .AddMicrosoftIdentityUI();
  }

  /// <summary>
  /// Adds authorization policies for role-based access control
  /// </summary>
  public static void AddAuthorization(WebApplicationBuilder builder)
  {
    builder.Services.AddAuthorizationBuilder()
      .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
      .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
      .AddPolicy("PowerUserOrAbove", policy => policy.RequireRole("Admin", "PowerUser"))
      .AddPolicy("AuthenticatedUser", policy => policy.RequireRole("Admin", "PowerUser", "User"));
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
