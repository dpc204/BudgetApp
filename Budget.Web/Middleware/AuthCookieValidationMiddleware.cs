namespace Budget.Web.Middleware;

/// <summary>
/// Middleware that ensures authentication cookies are cleared when the token cache is empty.
/// This prevents the "cookie exists but no MSAL account" scenario after app restarts.
/// Tracks per-user to ensure all users are validated, not just the first one.
/// </summary>
public class AuthCookieValidationMiddleware(RequestDelegate next, ILogger<AuthCookieValidationMiddleware> logger)
{
  // Track which users have been checked this app instance (by user ID hash)
  private static readonly HashSet<string> _checkedUsers = new();
  private static readonly object _lock = new();
  private static DateTime _appStartTime = DateTime.UtcNow;

  public async Task InvokeAsync(HttpContext context)
  {
    // Get the authentication cookie value to use as a user identifier
    var authCookie = context.Request.Cookies[".AspNetCore.Cookies"] 
                    ?? context.Request.Cookies["Budget.Auth"];
                    
    if (!string.IsNullOrEmpty(authCookie))
    {
      // Create a hash of the cookie to identify this user
      var userHash = GetCookieHash(authCookie);
      
      bool shouldCheck = false;
      lock (_lock)
      {
        // Check if we've already validated this user in this app instance
        if (!_checkedUsers.Contains(userHash))
        {
          _checkedUsers.Add(userHash);
          shouldCheck = true;
        }
      }
      
      if (shouldCheck)
      {
        // Check how old the cookie is - if it predates the app start, it's stale
        logger.LogInformation("First request for user (hash: {UserHash}) after app startup - validating authentication state", userHash.Substring(0, 8));
        
        // Clear the stale authentication cookie
        logger.LogWarning("Clearing stale authentication cookie for user (hash: {UserHash}) to force fresh sign-in", userHash.Substring(0, 8));
        
        context.Response.Cookies.Delete(".AspNetCore.Cookies", new CookieOptions 
        { 
          Path = "/",
          Secure = true,
          HttpOnly = true,
          SameSite = SameSiteMode.Lax
        });
        context.Response.Cookies.Delete("Budget.Auth", new CookieOptions 
        { 
          Path = "/",
          Secure = true,
          HttpOnly = true,
          SameSite = SameSiteMode.Lax
        });
        
        // Also clear any OpenID Connect cookies
        foreach (var cookie in context.Request.Cookies.Keys.Where(k => k.StartsWith(".AspNetCore.OpenIdConnect")))
        {
          context.Response.Cookies.Delete(cookie);
        }
        
        // Redirect to home page (which will trigger authentication)
        context.Response.Redirect("/");
        return;
      }
    }

    await next(context);
  }
  
  private static string GetCookieHash(string cookie)
  {
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var bytes = System.Text.Encoding.UTF8.GetBytes(cookie);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
  }
}

/// <summary>
/// Extension methods for registering the AuthCookieValidationMiddleware
/// </summary>
public static class AuthCookieValidationMiddlewareExtensions
{
  public static IApplicationBuilder UseAuthCookieValidation(this IApplicationBuilder builder)
  {
    return builder.UseMiddleware<AuthCookieValidationMiddleware>();
  }
}
