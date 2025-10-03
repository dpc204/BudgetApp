using Microsoft.AspNetCore.Components;

namespace Budget.Web.Components.Account
{
    internal sealed class IdentityRedirectManager(NavigationManager navigationManager, ILogger<IdentityRedirectManager> logger)
    {
        public void RedirectTo(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                uri = "/";
            }

            // Normalize to relative path (prevent open redirect)
            if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
            {
                uri = navigationManager.ToBaseRelativePath(uri);
            }

            if (string.IsNullOrWhiteSpace(uri) || uri == "/Account/Login")
            {
                uri = "/";
            }

            logger.LogInformation("Redirecting to: {Uri} from current URL: {CurrentUrl}", uri, navigationManager.Uri);

            // Works for both prerender (static) and interactive. In prerender, framework may short-circuit.
            navigationManager.NavigateTo(uri);
            // Do NOT throw – allows interactive auth flow.
        }

        public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
        {
            var basePath = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
            var newUri = navigationManager.GetUriWithQueryParameters(basePath, queryParameters);
            RedirectTo(newUri);
        }

        public void RedirectToWithStatus(string uri, string message, HttpContext context)
        {
            var uriWithStatus = navigationManager.GetUriWithQueryParameters(uri, new Dictionary<string, object?>
            {
                ["status"] = message
            });
            logger.LogInformation("Redirecting with status message to: {Uri}, Message: {Message}", uriWithStatus, message);
            RedirectTo(uriWithStatus);
        }

        private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

        public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

        public void RedirectToCurrentPageWithStatus(string message, HttpContext context) => RedirectToWithStatus(CurrentPath, message, context);
    }
}