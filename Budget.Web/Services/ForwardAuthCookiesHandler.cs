namespace Budget.Web.Services;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

/// <summary>
/// Forwards Entra ID access tokens to downstream API requests.
/// Budget.Api uses Entra ID JWT Bearer authentication.
/// 
/// NOTE: If token acquisition fails due to cache expiry (user_null), 
/// TokenCacheValidationMiddleware will force re-authentication on the next page load.
/// This handler simply returns 401 to signal the client that re-auth is needed.
/// </summary>
public sealed class ForwardAuthCookiesHandler(
  ITokenAcquisition tokenAcquisition,
  IConfiguration configuration,
  IHttpContextAccessor httpContextAccessor,
  ILogger<ForwardAuthCookiesHandler> logger) : DelegatingHandler
{
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    try
    {
      // Get the API scope from configuration
      var apiClientId = configuration["AzureAd:ClientId"];
      if (string.IsNullOrEmpty(apiClientId))
      {
        logger.LogWarning("AzureAd:ClientId not configured - cannot acquire token");
        return await base.SendAsync(request, cancellationToken);
      }

      var apiScope = $"api://{apiClientId}/access_as_user";
      logger.LogDebug("Attempting to acquire token for scope: {Scope}", apiScope);
      
      // Acquire access token for Budget.Api using OpenIdConnect scheme
      var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(
        [apiScope],
        authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);

      if (!string.IsNullOrEmpty(accessToken))
      {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        logger.LogDebug("? Added Bearer token for {Url} (token length: {Length})", 
          request.RequestUri, accessToken.Length);
      }
      else
      {
        logger.LogError("? Token acquisition returned null for {Url}", request.RequestUri);
        return CreateUnauthorizedResponse("Token acquisition returned null");
      }
    }
    catch (MsalUiRequiredException ex) when (ex.ErrorCode == "user_null")
    {
      // Token cache has no account data for this user
      // This happens when the distributed cache expires but the cookie is still valid
      // TokenCacheValidationMiddleware will force re-authentication on the next page load
      logger.LogWarning("? Token cache empty (user_null) for {Url}. User needs to re-authenticate.", request.RequestUri);
      
      // Mark the session as failed so middleware knows to force re-auth
      MarkSessionForReauth();
      
      return CreateUnauthorizedResponse("Session expired - refreshing authentication");
    }
    catch (MsalUiRequiredException ex)
    {
      logger.LogWarning(ex, "? MSAL UI interaction required for {Url}. ErrorCode: {ErrorCode}", 
        request.RequestUri, ex.ErrorCode);
      
      MarkSessionForReauth();
      return CreateUnauthorizedResponse($"Authentication required ({ex.ErrorCode})");
    }
    catch (MicrosoftIdentityWebChallengeUserException ex)
    {
      logger.LogWarning(ex, "? User challenge required for {Url}. MsalError: {Error}",
        request.RequestUri, ex.MsalUiRequiredException?.ErrorCode);
      
      MarkSessionForReauth();
      return CreateUnauthorizedResponse("User challenge required");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "? Error acquiring access token for {Url}: {Message}", 
        request.RequestUri, ex.Message);
      
      return CreateUnauthorizedResponse("Token acquisition failed");
    }

    return await base.SendAsync(request, cancellationToken);
  }

  private void MarkSessionForReauth()
  {
    // Get session ID and mark it for re-authentication
    var httpContext = httpContextAccessor.HttpContext;
    if (httpContext?.User.Identity?.IsAuthenticated == true)
    {
      var sessionId = httpContext.User.FindFirst("sid")?.Value ?? 
                      httpContext.User.FindFirst("oid")?.Value ??
                      httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
      
      if (!string.IsNullOrEmpty(sessionId))
      {
        // Clear validation so next page load triggers re-auth
        Budget.Web.Middleware.TokenCacheValidationMiddleware.ClearSessionValidation(sessionId);
        logger.LogInformation("Marked session {SessionId} for re-authentication", sessionId);
      }
    }
  }

  private static HttpResponseMessage CreateUnauthorizedResponse(string reason)
  {
    return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
    {
      ReasonPhrase = reason,
      Content = new StringContent($"{{\"error\": \"{reason}\", \"action\": \"Please refresh the page to sign in again.\"}}")
    };
  }
}