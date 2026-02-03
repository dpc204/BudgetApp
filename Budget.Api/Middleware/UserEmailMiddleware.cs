using System.Security.Claims;
using Budget.Shared.Services;

namespace Budget.Api.Middleware;

/// <summary>
/// Middleware that extracts the authenticated user's email from token claims
/// and initializes UserAndOptions for lazy loading.
/// The actual user info will be loaded from the database only when first accessed.
/// </summary>
public sealed class UserEmailMiddleware(
  RequestDelegate next,
  ILogger<UserEmailMiddleware> logger)
{
  public async Task InvokeAsync(HttpContext context)
  {
    // Skip for:
    // - Unauthenticated users
    // - Static files (have extensions)
    // - Health checks
    // - OpenAPI/Swagger endpoints
    if (!context.User.Identity?.IsAuthenticated ?? true ||
        context.Request.Path.StartsWithSegments("/health") ||
        context.Request.Path.StartsWithSegments("/openapi") ||
        context.Request.Path.StartsWithSegments("/scalar") ||
        context.Request.Path.Value?.Contains(".") == true)
    {
      await next(context);
      return;
    }

    // Extract email from token claims (same logic as other parts of the system)
    var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User.FindFirst("preferred_username")?.Value
                ?? context.User.FindFirst("upn")?.Value;

    if (!string.IsNullOrEmpty(email))
    {
      // Get scoped UserAndOptions service and set email for lazy loading
      var userAndOptions = context.RequestServices.GetService<IUserAndOptions>();
      
      if (userAndOptions != null)
      {
        userAndOptions.SetUserEmail(email);
        logger.LogDebug("Set user email for lazy loading: {Email}", email);
      }
      else
      {
        logger.LogWarning("IUserAndOptions service not available in request scope");
      }
    }
    else
    {
      logger.LogDebug("No email claim found for authenticated user");
    }

    await next(context);
  }
}
