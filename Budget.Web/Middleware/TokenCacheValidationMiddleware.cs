using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace Budget.Web.Middleware;

/// <summary>
/// Validates that authenticated users have a valid token in the cache.
/// This prevents 401 errors when the app restarts but the browser cookie remains valid.
/// </summary>
public sealed class TokenCacheValidationMiddleware(
  RequestDelegate next,
  ILogger<TokenCacheValidationMiddleware> logger)
{
  private static readonly HashSet<string> _validatedSessions = [];
  private static readonly object _lock = new();

  public async Task InvokeAsync(
    HttpContext context,
    ITokenAcquisition tokenAcquisition,
    IConfiguration configuration)
  {
    // Skip validation for:
    // - Unauthenticated users
    // - Sign-in/sign-out endpoints (including OAuth callback)
    // - Static files
    // - Health checks
    // - API endpoints (they have their own auth)
    if (!context.User.Identity?.IsAuthenticated ?? true ||
        context.Request.Path.StartsWithSegments("/MicrosoftIdentity") ||
        context.Request.Path.StartsWithSegments("/signin-oidc") ||
        context.Request.Path.StartsWithSegments("/_framework") ||
        context.Request.Path.StartsWithSegments("/health") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value?.Contains(".") == true) // Static files have extensions
    {
      await next(context);
      return;
    }

    // Get session identifier (use the auth cookie's session ID)
    var sessionId = context.User.FindFirst("sid")?.Value ?? 
                    context.User.FindFirst("oid")?.Value ?? 
                    "unknown";

    // CRITICAL: If sessionId is "unknown", the user just authenticated but claims aren't populated yet
    // Skip validation and let the session establish itself
    if (sessionId == "unknown")
    {
      logger.LogDebug("Skipping token cache validation - session ID not yet available");
      await next(context);
      return;
    }

    // Check if this session has already been validated (to avoid repeated checks)
    bool alreadyValidated;
    lock (_lock)
    {
      alreadyValidated = _validatedSessions.Contains(sessionId);
    }

    if (alreadyValidated)
    {
      await next(context);
      return;
    }

    // Validate that the token cache has a token for this user
    try
    {
      var apiClientId = configuration["AzureAd:ClientId"];
      if (string.IsNullOrEmpty(apiClientId))
      {
        logger.LogWarning("AzureAd:ClientId not configured - skipping token cache validation");
        await next(context);
        return;
      }

      var apiScope = $"api://{apiClientId}/access_as_user";

      // Try to get a token - this will fail if the cache is empty
#pragma warning disable IDE0300 // Simplify collection initialization
      var token = await tokenAcquisition.GetAccessTokenForUserAsync(
        new[] { apiScope },
        authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);
#pragma warning restore IDE0300 // Simplify collection initialization

      if (string.IsNullOrEmpty(token))
      {
        logger.LogWarning("Token cache validation failed - no token available for session {SessionId}. Allowing request to proceed (API will return 401 if needed).", sessionId);
        
        // Don't force sign-out immediately - let the API call fail naturally
        // The user will be prompted to sign in when they actually try to use the API
        await next(context);
        return;
      }

      // Token acquired successfully - mark session as validated
      lock (_lock)
      {
        _validatedSessions.Add(sessionId);
      }

      logger.LogDebug("Token cache validated successfully for session {SessionId}", sessionId);
    }
    catch (MsalUiRequiredException ex)
    {
      logger.LogWarning(ex, "Token cache validation failed - MSAL UI required (ErrorCode: {ErrorCode}) for session {SessionId}. Allowing request to proceed.", 
        ex.ErrorCode, sessionId);
      
      // Don't force sign-out - let the user continue and handle auth at the API level
      await next(context);
      return;
    }
    catch (MicrosoftIdentityWebChallengeUserException ex)
    {
      logger.LogWarning(ex, "Token cache validation failed - user challenge required for session {SessionId}. Allowing request to proceed.", sessionId);
      
      // Don't force sign-out - let the user continue
      await next(context);
      return;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error during token cache validation for session {SessionId}. Allowing request to proceed.", sessionId);
      
      // Don't force sign-out on unexpected errors - just log and continue
      await next(context);
      return;
    }

    await next(context);
  }
}
