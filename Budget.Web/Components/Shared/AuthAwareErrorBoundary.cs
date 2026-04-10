using Microsoft.AspNetCore.Components;

namespace Budget.Web.Components.Shared;

/// <summary>
/// Custom error boundary that handles authentication-related errors gracefully
/// </summary>
public class AuthAwareErrorBoundary : ErrorBoundary
{
  [Inject] private ILogger<AuthAwareErrorBoundary> Logger { get; set; } = default!;
  [Inject] private NavigationManager Navigation { get; set; } = default!;

  protected override Task OnErrorAsync(Exception exception)
  {
    Logger.LogError(exception, "Error boundary caught exception: {Message}", exception.Message);

    // Check if this is an authentication-related error
    var isAuthError = exception is HttpRequestException httpEx &&
                      (httpEx.Message.Contains("401") ||
                       httpEx.Message.Contains("Unauthorized") ||
                       httpEx.Message.Contains("consent required") ||
                       httpEx.Message.Contains("sign out and sign back in"));

    if(isAuthError)
    {
      Logger.LogWarning("Authentication error detected - user needs to sign out and sign in");

      // Set a flag in session storage to show a message after redirect
      // This would require JS interop, so for now just log it
    }

    return base.OnErrorAsync(exception);
  }
}
