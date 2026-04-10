using Microsoft.Identity.Client;

namespace Budget.Web.Middleware;

/// <summary>
/// Validates that authenticated users have a valid token in the cache.
/// When the token cache is empty/expired (user_null error), forces re-authentication
/// to repopulate the cache with account data and refresh tokens.
/// </summary>
public sealed class TokenCacheValidationMiddleware(
  RequestDelegate next,
  ILogger<TokenCacheValidationMiddleware> logger)
{
  private static readonly HashSet<string> _validatedSessions = [];
  private static readonly HashSet<string> _failedSessions = []; // Track sessions that failed validation
  private static readonly Lock _lock = new();

  /// <summary>
  /// Clears validation state for a session (call after successful re-auth)
  /// </summary>
  public static void ClearSessionValidation(string sessionId)
  {
    lock(_lock)
    {
      _validatedSessions.Remove(sessionId);
      _failedSessions.Remove(sessionId);
    }
  }

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
    // - SignalR connections (Blazor Server uses SignalR)
    if(!context.User.Identity?.IsAuthenticated ?? true ||
        context.Request.Path.StartsWithSegments("/MicrosoftIdentity") ||
        context.Request.Path.StartsWithSegments("/signin-oidc") ||
        context.Request.Path.StartsWithSegments("/_framework") ||
        context.Request.Path.StartsWithSegments("/_blazor") ||
        context.Request.Path.StartsWithSegments("/health") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value?.Contains('.') == true) // Static files have extensions
    {
      await next(context);
      return;
    }

    // Get session identifier (use the auth cookie's session ID or object ID)
    var sessionId = context.User.FindFirst("sid")?.Value ??
                    context.User.FindFirst("oid")?.Value ??
                    context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
                    "unknown";

    // CRITICAL: If sessionId is "unknown", the user just authenticated but claims aren't populated yet
    // Skip validation and let the session establish itself
    if(sessionId == "unknown")
    {
      logger.LogDebug("Skipping token cache validation - session ID not yet available");
      await next(context);
      return;
    }

    // Check if this session has already been validated (to avoid repeated checks)
    bool alreadyValidated;
    bool previouslyFailed;
    lock(_lock)
    {
      alreadyValidated = _validatedSessions.Contains(sessionId);
      previouslyFailed = _failedSessions.Contains(sessionId);
    }

    if(alreadyValidated)
    {
      await next(context);
      return;
    }

    // If this session previously failed and we haven't cleared it, force re-auth
    if(previouslyFailed)
    {
      logger.LogWarning("Session {SessionId} previously failed token validation - forcing re-authentication", sessionId);
      await ForceReauthenticationAsync(context, "Token cache expired - please sign in again");
      return;
    }

    // Validate that the token cache has a token for this user
    try
    {
      var apiClientId = configuration["AzureAd:ClientId"];
      if(string.IsNullOrEmpty(apiClientId))
      {
        logger.LogWarning("AzureAd:ClientId not configured - skipping token cache validation");
        await next(context);
        return;
      }

      var apiScope = $"api://{apiClientId}/access_as_user";

      // Try to get a token - this will fail if the cache is empty
      var token = await tokenAcquisition.GetAccessTokenForUserAsync(
        [apiScope],
        authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);

      if(string.IsNullOrEmpty(token))
      {
        logger.LogWarning("Token cache validation failed - no token available for session {SessionId}. Forcing re-authentication.", sessionId);
        MarkSessionFailed(sessionId);
        await ForceReauthenticationAsync(context, "No token in cache");
        return;
      }

      // Token acquired successfully - mark session as validated
      lock(_lock)
      {
        _validatedSessions.Add(sessionId);
        _failedSessions.Remove(sessionId);
      }

      logger.LogDebug("Token cache validated successfully for session {SessionId}", sessionId);
    }
    catch(MsalUiRequiredException ex) when(ex.ErrorCode == "user_null")
    {
      // CRITICAL: user_null means the token cache has no account data for this user
      // This happens when the cache expires but the cookie is still valid
      // We MUST force re-authentication to repopulate the cache
      logger.LogWarning("Token cache empty (user_null) for session {SessionId}. Forcing re-authentication to repopulate cache.", sessionId);
      MarkSessionFailed(sessionId);
      await ForceReauthenticationAsync(context, "Token cache expired");
      return;
    }
    catch(MsalUiRequiredException ex)
    {
      // Other MSAL errors that require UI interaction
      logger.LogWarning(ex, "Token cache validation failed - MSAL UI required (ErrorCode: {ErrorCode}) for session {SessionId}. Forcing re-authentication.",
        ex.ErrorCode, sessionId);
      MarkSessionFailed(sessionId);
      await ForceReauthenticationAsync(context, $"MSAL error: {ex.ErrorCode}");
      return;
    }
    catch(MicrosoftIdentityWebChallengeUserException ex)
    {
      logger.LogWarning(ex, "Token cache validation failed - user challenge required for session {SessionId}. Forcing re-authentication.", sessionId);
      MarkSessionFailed(sessionId);
      await ForceReauthenticationAsync(context, "User challenge required");
      return;
    }
    catch(Exception ex)
    {
      logger.LogError(ex, "Unexpected error during token cache validation for session {SessionId}. Allowing request to proceed.", sessionId);

      // For unexpected errors, don't block - let the request continue
      // The API call will fail with 401 if there's truly a problem
      await next(context);
      return;
    }

    await next(context);
  }

  private static void MarkSessionFailed(string sessionId)
  {
    lock(_lock)
    {
      _failedSessions.Add(sessionId);
      _validatedSessions.Remove(sessionId);
    }
  }

  private async Task ForceReauthenticationAsync(HttpContext context, string reason)
  {
    logger.LogInformation("Forcing re-authentication for user {User}. Reason: {Reason}",
      context.User.Identity?.Name ?? "unknown", reason);

    // Sign out the user from cookie auth to clear the stale session
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    // Challenge with OpenID Connect to trigger a fresh sign-in
    // This will redirect to Entra ID and repopulate the token cache
    var redirectUri = context.Request.Path + context.Request.QueryString;

    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties {
      RedirectUri = redirectUri,
      IsPersistent = true
    });
  }
}
