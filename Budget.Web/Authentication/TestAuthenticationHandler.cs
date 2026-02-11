using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Budget.Web.Authentication;

/// <summary>
/// Test authentication handler that bypasses Entra ID for automated testing
/// Only enabled when USE_TEST_AUTH environment variable is set to "true"
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  public const string AuthenticationScheme = "TestScheme";

  public TestAuthenticationHandler(
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
      new Claim("name", "Test User"),
      new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "test-user-id-12345")
    };

    var identity = new ClaimsIdentity(claims, AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

    Logger.LogInformation("? Test authentication successful for Test User");

    return Task.FromResult(AuthenticateResult.Success(ticket));
  }
}
