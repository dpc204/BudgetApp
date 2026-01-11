namespace Budget.Web.Services;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Budget.Web.Services;

/// <summary>
/// Forwards Entra ID access tokens to downstream API requests.
/// Budget.Api uses Entra ID JWT Bearer authentication.
/// Includes automatic stale token detection and cache clearing.
/// </summary>
public sealed class ForwardAuthCookiesHandler(
  ITokenAcquisition tokenAcquisition,
  IConfiguration configuration,
  TokenCacheManager tokenCacheManager,
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
      logger.LogInformation("Attempting to acquire token for scope: {Scope}", apiScope);
      
      // Acquire access token for Budget.Api using OpenIdConnect scheme
      // Must specify the scheme because ITokenAcquisition needs to know which token cache to use
#pragma warning disable IDE0300 // Simplify collection initialization
      var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(
        new[] { apiScope },
        authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);
#pragma warning restore IDE0300 // Simplify collection initialization

      if (!string.IsNullOrEmpty(accessToken))
      {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        logger.LogInformation("? Added Bearer token for {Url} (token length: {Length})", 
          request.RequestUri, accessToken.Length);
        
        // DIAGNOSTIC: Decode and log token claims to troubleshoot 403 errors
        try
        {
          var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
          var jsonToken = handler.ReadToken(accessToken) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
          var roles = jsonToken?.Claims.Where(c => c.Type == "roles" || c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value).ToList();
          
          logger.LogWarning("JWT Token roles: {Roles}", roles != null && roles.Any() ? string.Join(", ", roles) : "NONE");
        }
        catch (Exception ex)
        {
          logger.LogDebug(ex, "Could not decode JWT token");
        }
      }
      else
      {
        logger.LogError("? Failed to acquire access token for {Url}", request.RequestUri);
      }
    }
    catch (MicrosoftIdentityWebChallengeUserException ex)
    {
      // User needs to consent or re-authenticate
      logger.LogError(ex, "? User consent required for {Url}. MsalError: {Error}",
        request.RequestUri, ex.MsalUiRequiredException?.ErrorCode);
      
      // Auto-detect and clear stale tokens (synchronously to avoid unhandled exceptions)
      var errorCode = ex.MsalUiRequiredException?.ErrorCode ?? "unknown";
      if (tokenCacheManager.ShouldClearCache(errorCode, "consent required"))
      {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        
        try
        {
          // Clear cache synchronously to handle errors properly
          var cleared = await tokenCacheManager.HandleStaleTokenAsync(userId, cancellationToken);
          if (cleared)
          {
            logger.LogWarning("? Stale token cache cleared. User will be prompted to sign in again.");
          }
          else
          {
            logger.LogWarning("? Token cache clear was skipped or failed. User may need to manually clear cookies.");
          }
        }
        catch (Exception clearEx)
        {
          logger.LogError(clearEx, "? Exception while clearing token cache - continuing with 401 response");
        }
      }
      
      // Don't proceed without a token - return 401 with clear error
      var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
      {
        ReasonPhrase = "Authentication required - please sign out and sign in again"
      };
      return response;
    }
    catch (MsalUiRequiredException ex)
    {
      // MSAL requires UI interaction (consent, MFA, etc.)
      logger.LogError(ex, "? MSAL UI interaction required for {Url}. Error: {Error}, ErrorCode: {ErrorCode}",
        request.RequestUri, ex.Message, ex.ErrorCode);
      
      // Auto-detect and clear stale tokens (synchronously to avoid unhandled exceptions)
      if (tokenCacheManager.ShouldClearCache(ex.ErrorCode, ex.Message))
      {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        
        try
        {
          // Clear cache synchronously to handle errors properly
          var cleared = await tokenCacheManager.HandleStaleTokenAsync(userId, cancellationToken);
          if (cleared)
          {
            logger.LogWarning("? Stale token cache cleared. User will be prompted to sign in again.");
          }
          else
          {
            logger.LogWarning("? Token cache clear was skipped or failed. User may need to manually clear cookies.");
          }
        }
        catch (Exception clearEx)
        {
          logger.LogError(clearEx, "? Exception while clearing token cache - continuing with 401 response");
        }
      }
      
      var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
      {
        ReasonPhrase = $"Authentication required - please sign out and sign in again"
      };
      return response;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "? Error acquiring access token for {Url}. Exception: {Message}", 
        request.RequestUri, ex.Message);
      
      // Don't proceed without a token
      var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
      {
        ReasonPhrase = "Token acquisition failed"
      };
      return response;
    }

    return await base.SendAsync(request, cancellationToken);
  }
}