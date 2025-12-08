namespace Budget.Web.Services;

/// <summary>
/// Forwards Entra ID access tokens to downstream API requests
/// </summary>
public sealed class ForwardAuthCookiesHandler(
  IHttpContextAccessor httpContextAccessor,
  ITokenAcquisition tokenAcquisition,
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
        // Try to get access token for the API
        // The scopes should be configured based on your API's requirements
        var scopes = new[] { "api://budget-api/.default" };
        
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        
        if (!string.IsNullOrEmpty(accessToken))
        {
          // Add the access token to the Authorization header
          request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
          logger.LogDebug("Added Bearer token to request for {Url}", request.RequestUri);
        }
        else
        {
          logger.LogWarning("Failed to acquire access token for API call to {Url}", request.RequestUri);
        }
      }
      catch (Exception ex)
      {
        // If we can't get a token, log the error but don't fail the request
        // Fall back to cookie forwarding for backward compatibility during migration
        logger.LogWarning(ex, "Error acquiring access token, falling back to cookie forwarding for {Url}", request.RequestUri);
        
        // Fallback: Forward cookies for backward compatibility during migration
        if (ctx.Request.Headers.TryGetValue("Cookie", out var cookie))
        {
          if (!request.Headers.Contains("Cookie"))
          {
            var cookieArray = cookie.ToArray();
            request.Headers.TryAddWithoutValidation("Cookie", cookieArray);
            logger.LogDebug("Forwarded cookies as fallback for {Url}", request.RequestUri);
          }
        }
      }
    }

    return await base.SendAsync(request, cancellationToken);
  }
}