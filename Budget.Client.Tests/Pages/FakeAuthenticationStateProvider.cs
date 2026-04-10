using Microsoft.AspNetCore.Components.Authorization;

namespace Budget.Client.Tests.Pages;

/// <summary>
/// Fake authentication state provider for testing
/// </summary>
public class FakeAuthenticationStateProvider : AuthenticationStateProvider
{
  public override Task<AuthenticationState> GetAuthenticationStateAsync()
  {
#pragma warning disable IDE0300 // Simplify collection initialization
    var identity = new System.Security.Claims.ClaimsIdentity(new[]
    {
      new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User"),
      new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "test@example.com")
    }, "Test");
#pragma warning restore IDE0300 // Simplify collection initialization

    var user = new System.Security.Claims.ClaimsPrincipal(identity);
    return Task.FromResult(new AuthenticationState(user));
  }
}