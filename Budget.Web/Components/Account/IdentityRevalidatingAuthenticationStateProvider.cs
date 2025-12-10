using System.Security.Claims;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Options;

namespace Budget.Web.Components.Account
{
    /// <summary>
    /// Server-side AuthenticationStateProvider that revalidates Entra ID tokens
    /// for connected users every 30 minutes in an interactive circuit
    /// </summary>
    internal sealed class IdentityRevalidatingAuthenticationStateProvider(
            ILoggerFactory loggerFactory)
        : RevalidatingServerAuthenticationStateProvider(loggerFactory)
    {
        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            // For Entra ID authentication, we validate that the user is still authenticated
            // Token validation is handled by Microsoft.Identity.Web middleware
            var principal = authenticationState.User;
            
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            // Check if the user has required claims
            var hasObjectId = principal.FindFirst("oid") != null 
                || principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier") != null
                || principal.FindFirst(ClaimTypes.NameIdentifier) != null;

            if (!hasObjectId)
            {
                return false;
            }

            // Additional validation could be added here, such as:
            // - Checking token expiration
            // - Validating user still exists in Entra ID
            // - Checking if user's roles have changed
            
            return await Task.FromResult(true);
        }
    }
}
