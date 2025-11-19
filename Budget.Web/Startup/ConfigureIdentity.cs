using Budget.Shared.Models;
using Budget.Web.Components.Account;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Budget.Web.Startup;

/// <summary>
/// Configures ASP.NET Core Identity and authentication services
/// </summary>
public static class ConfigureIdentity
{
  /// <summary>
  /// Adds authentication with Identity cookies configured for API-style responses
  /// </summary>
  public static void AddAuthentication(WebApplicationBuilder builder)
  {
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
  }

  /// <summary>
  /// Adds authorization policies
  /// </summary>
  public static void AddAuthorization(WebApplicationBuilder builder)
  {
    builder.Services.AddAuthorization(options =>
    {
      options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    });
  }

  /// <summary>
  /// Configures ASP.NET Core Identity with roles and services
  /// </summary>
  public static void AddIdentityCore(WebApplicationBuilder builder)
  {
    builder.Services.AddScoped<IdentityUserAccessor>();
    builder.Services.AddScoped<IdentityRedirectManager>();
    builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

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
  }
}
