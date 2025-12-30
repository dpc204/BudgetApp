namespace Budget.Web.Services;

/// <summary>
/// Forwards authentication cookies to downstream API requests.
/// Budget.Api uses cookie-based authentication, not Entra ID tokens.
/// </summary>
public sealed class ForwardAuthCookiesHandler(
  IHttpContextAccessor httpContextAccessor,
  ILogger<ForwardAuthCookiesHandler> logger) : DelegatingHandler
{
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    var ctx = httpContextAccessor.HttpContext;
    
    // Forward authentication cookies to Budget.Api
    if (ctx is not null && ctx.User?.Identity?.IsAuthenticated == true)
    {
      if (ctx.Request.Headers.TryGetValue("Cookie", out var cookie))
      {
        if (!request.Headers.Contains("Cookie"))
        {
          var cookieArray = cookie.ToArray();
          request.Headers.TryAddWithoutValidation("Cookie", cookieArray);
          logger.LogDebug("Forwarding authentication cookies for {Url}", request.RequestUri);
        }
      }
    }

    return await base.SendAsync(request, cancellationToken);
  }
}