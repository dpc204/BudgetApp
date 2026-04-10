using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Budget.ApiTests;

/// <summary>
/// Test authentication handler that automatically authenticates all requests for integration testing
/// </summary>
public class TestAuthHandler(
  IOptionsMonitor<AuthenticationSchemeOptions> options,
  ILoggerFactory logger,
  UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    Claim[] claims =
    [
      new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
      new Claim(ClaimTypes.Name, "Test User"),
      new Claim(ClaimTypes.Email, "test@example.com"),
      new Claim(ClaimTypes.Role, "Admin")
    ];

    var identity = new ClaimsIdentity(claims, "Test");
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, "Test");

    return Task.FromResult(AuthenticateResult.Success(ticket));
  }
}
