using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace Budget.Web.Components.Account
{
    internal sealed class IdentityRedirectManager(NavigationManager navigationManager, ILogger<IdentityRedirectManager> logger)
    {
        [DoesNotReturn]
        public void RedirectTo(string? uri)
        {
            // Default to home page if uri is null or empty
            if (string.IsNullOrWhiteSpace(uri))
            {
                uri = "/";
            }

            // Prevent open redirects.
            if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
            {
                uri = navigationManager.ToBaseRelativePath(uri);
            }

            // If we still don't have a valid relative URI, default to home
            if (string.IsNullOrWhiteSpace(uri) || uri == "/Account/Login")
            {
                uri = "/";
            }

            // Log the redirect for debugging
            logger.LogInformation("Redirecting to: {Uri} from current URL: {CurrentUrl}", uri, navigationManager.Uri);

            try
            {
                // During static rendering, NavigateTo throws a NavigationException which is handled by the framework as a redirect.
                navigationManager.NavigateTo(uri);
                // If we reach this point, we're not in static rendering, so we need to force a full page redirect
                throw new InvalidOperationException($"{nameof(IdentityRedirectManager)} can only be used during static rendering.");
            }
            catch (InvalidOperationException) when (navigationManager.Uri != null)
            {
                // If NavigateTo fails because we're not in static rendering context,
                // try to force a full page navigation using JavaScript
                logger.LogWarning("NavigateTo failed in non-static context, attempting JavaScript redirect to: {Uri}", uri);
                navigationManager.NavigateTo(uri, forceLoad: true);
            }
        }

        [DoesNotReturn]
        public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
        {
            var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
            var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
            RedirectTo(newUri);
        }

        [DoesNotReturn]
        public void RedirectToWithStatus(string uri, string message, HttpContext context)
        {
            // For Blazor applications, encode the status message in the URL query string
            // instead of using cookies which are unreliable with mixed rendering modes
            var uriWithStatus = navigationManager.GetUriWithQueryParameters(uri, 
                new Dictionary<string, object?> { ["status"] = message });
            
            logger.LogInformation("Redirecting with status message to: {Uri}, Message: {Message}", uriWithStatus, message);
            RedirectTo(uriWithStatus);
        }

        private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

        [DoesNotReturn]
        public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

        [DoesNotReturn]
        public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
            => RedirectToWithStatus(CurrentPath, message, context);
    }
}