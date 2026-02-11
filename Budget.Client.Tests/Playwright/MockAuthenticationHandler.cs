using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Budget.Client.Tests.Playwright;

/// <summary>
/// Mock authentication handler that bypasses Entra ID authentication for testing
/// </summary>
public class MockAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  public const string AuthenticationScheme = "TestScheme";

  public MockAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : base(options, logger, encoder)
  {
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    var claims = new[]
    {
      new Claim(ClaimTypes.Name, "Test User"),
      new Claim(ClaimTypes.Email, "dpc204@gmail.com"),
      new Claim(ClaimTypes.NameIdentifier, "test-user-id-12345"),
      new Claim("preferred_username", "dpc204@gmail.com"),
      new Claim("name", "Test User")
    };

    var identity = new ClaimsIdentity(claims, AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

    return Task.FromResult(AuthenticateResult.Success(ticket));
  }
}
