using Microsoft.Identity.Client;

namespace Budget.Web.Services;

/// <summary>
/// Forwards Entra ID access tokens to downstream API requests
/// </summary>
public sealed class ForwardAuthCookiesHandler(
  IHttpContextAccessor httpContextAccessor,
  ITokenAcquisition tokenAcquisition,
  IConfiguration configuration,
  ILogger<ForwardAuthCookiesHandler> logger) : DelegatingHandler
{
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    var ctx = httpContextAccessor.HttpContext;
    
    if (ctx is not null && ctx.User?.Identity?.IsAuthenticated == true)
    {
      try
      {
        // Get API scope from configuration, with fallback to default
        var apiScope = configuration["AzureAd:ApiScope"] ?? "api://budget-api/.default";
        var scopes = new[] { apiScope };
        
        // Try to get access token silently (from cache)
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(
          scopes, 
          authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);
        
        if (!string.IsNullOrEmpty(accessToken))
        {
          // Add the access token to the Authorization header
          request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
          logger.LogDebug("Added Bearer token to request for {Url}", request.RequestUri);
          return await base.SendAsync(request, cancellationToken);
        }
      }
      catch (MicrosoftIdentityWebChallengeUserException)
      {
        // User needs to consent to access the API
        // This is expected on first API call after sign-in with incremental consent
        // Fall through to cookie-based auth (backward compatibility)
        logger.LogDebug("Token acquisition requires user consent, using cookie-based auth for {Url}", request.RequestUri);
      }
      catch (MsalUiRequiredException)
      {
        // No cached token available and user interaction is required
        // Fall through to cookie-based auth (backward compatibility)
        logger.LogDebug("No cached token available, using cookie-based auth for {Url}", request.RequestUri);
      }
      catch (Exception ex)
      {
        // Log unexpected errors but continue with cookie fallback
        logger.LogWarning(ex, "Unexpected error acquiring access token for {Url}, using cookie-based auth", request.RequestUri);
      }
      
      // Fallback: Forward cookies for backward compatibility during migration
      if (ctx.Request.Headers.TryGetValue("Cookie", out var cookie))
      {
        if (!request.Headers.Contains("Cookie"))
        {
          var cookieArray = cookie.ToArray();
          request.Headers.TryAddWithoutValidation("Cookie", cookieArray);
          logger.LogDebug("Using cookie-based authentication for {Url}", request.RequestUri);
        }
      }
    }

    return await base.SendAsync(request, cancellationToken);
  }
}