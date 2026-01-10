using Budget.Web.Components.Account;
using Budget.Web.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Budget.Web.Startup;

/// <summary>
/// Configures the middleware pipeline and endpoint routing
/// </summary>
public static class ConfigureMiddleware
{
  /// <summary>
  /// Configures static files with development-specific cache settings
  /// </summary>
  public static void ConfigureStaticFiles(WebApplication app)
  {
    if (app.Environment.IsDevelopment())
    {
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
      app.UseStaticFiles();
    }
  }

  /// <summary>
  /// Configures exception handling and HSTS
  /// </summary>
  public static void ConfigureExceptionHandling(WebApplication app)
  {
    if (app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
      app.UseMigrationsEndPoint();
    }
    else
    {
      app.UseExceptionHandler("/Error", createScopeForErrors: true);
      app.UseHsts();
      app.UseMigrationsEndPoint();
    }
  }

  /// <summary>
  /// Configures the middleware pipeline
  /// </summary>
  public static void ConfigurePipeline(WebApplication app)
  {
    // Apply request localization middleware for culture-specific formatting
    app.UseRequestLocalization();
    
    app.UseStatusCodePagesWithReExecute("/not-found");
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();
  }

  /// <summary>
  /// Maps endpoints including Razor Components and health checks
  /// </summary>
  public static void MapEndpoints(WebApplication app)
  {
    app.MapStaticAssets();

    app.MapRazorComponents<App>()
      .AddInteractiveServerRenderMode()
      .AddAdditionalAssemblies(typeof(Budget.Client.Pages.Home).Assembly);

    // Map controllers for Microsoft Identity UI
    app.MapControllers();

    app.MapAdditionalIdentityEndpoints();

    // Health endpoints (development only for security)
    if (app.Environment.IsDevelopment())
    {
      app.MapHealthChecks("/health");
      app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
    }
  }
}
