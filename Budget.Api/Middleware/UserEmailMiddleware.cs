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

        // Check for UserId and FamilyId in custom headers (sent by Budget.Web)
        if (context.Request.Headers.TryGetValue("X-UserId", out var userIdHeader) &&
            context.Request.Headers.TryGetValue("X-FamilyId", out var familyIdHeader))
        {
          if (int.TryParse(userIdHeader.ToString(), out var userId) &&
              int.TryParse(familyIdHeader.ToString(), out var familyId))
          {
            userAndOptions.SetUserIdAndFamilyId(userId, familyId);
            logger.LogDebug("Set UserId: {UserId}, FamilyId: {FamilyId} from headers", userId, familyId);
          }
          else
          {
            logger.LogWarning("Failed to parse UserId or FamilyId from headers");
          }
        }
        else
        {
          logger.LogDebug("X-UserId or X-FamilyId headers not found - will load from database if needed");
        }
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
