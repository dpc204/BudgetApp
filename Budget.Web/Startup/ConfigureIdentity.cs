using Budget.Web.Components.Account;

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
    // Configure Microsoft Entra ID authentication
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
      .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"),
        cookieScheme: null,
        openIdConnectScheme: OpenIdConnectDefaults.AuthenticationScheme);

    // Configure cookie authentication options for Blazor Server
    builder.Services.ConfigureApplicationCookie(options =>
    {
      options.Cookie.HttpOnly = true;
      options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
      options.Cookie.SameSite = SameSiteMode.Lax;
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
