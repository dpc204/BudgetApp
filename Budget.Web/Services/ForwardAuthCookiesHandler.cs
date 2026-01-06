namespace Budget.Web.Services;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

/// <summary>
/// Forwards Entra ID access tokens to downstream API requests.
/// Budget.Api uses Entra ID JWT Bearer authentication.
/// </summary>
public sealed class ForwardAuthCookiesHandler(
  ITokenAcquisition tokenAcquisition,
  IConfiguration configuration,
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
      var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(
        new[] { apiScope },
        authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);

      if (!string.IsNullOrEmpty(accessToken))
      {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        logger.LogInformation("? Added Bearer token for {Url} (token length: {Length})", 
          request.RequestUri, accessToken.Length);
      }
      else
      {
        logger.LogError("? Failed to acquire access token for {Url}", request.RequestUri);
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "? Error acquiring access token for {Url}. Exception: {Message}", 
        request.RequestUri, ex.Message);
    }

    return await base.SendAsync(request, cancellationToken);
  }
}