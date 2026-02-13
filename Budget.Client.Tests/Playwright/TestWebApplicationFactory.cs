using Budget.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Budget.Client.Tests.Playwright;

/// <summary>
/// Custom WebApplicationFactory for hosting the Blazor app with mock authentication during tests
/// </summary>
public class TestWebApplicationFactory(string environment = "Development") : WebApplicationFactory<ProgramMarker>
{
  private IHost? _host;

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment(environment);

    builder.ConfigureTestServices(services =>
    {
      // Remove existing authentication schemes
      services.RemoveAll<IAuthenticationSchemeProvider>();
      services.AddSingleton<IAuthenticationSchemeProvider, MockAuthenticationSchemeProvider>();

      // Register mock authentication handler
      services.AddAuthentication(MockAuthenticationHandler.AuthenticationScheme)
        .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>(
          MockAuthenticationHandler.AuthenticationScheme,
          options => { });

      // Override any other services needed for testing
      // Example: Replace API client with mock if needed
    });
  }

  protected override IHost CreateHost(IHostBuilder builder)
  {
    // Create the TestServer host (for HttpClient testing)
    var testHost = builder.Build();
    testHost.Start();

    // Create a separate Kestrel host for Playwright browser testing
    var kestrelBuilder = Host.CreateDefaultBuilder()
      .ConfigureWebHost(webHostBuilder =>
      {
        webHostBuilder.UseKestrel();
        webHostBuilder.UseUrls("http://127.0.0.1:0"); // Use any available port
        webHostBuilder.UseEnvironment(environment);
        webHostBuilder.Configure(app =>
        {
          // Use the same configuration as the main app
          var appHost = testHost.Services.GetRequiredService<IHost>();
          // Copy the app configuration from testHost
        });
        
        // Apply the same service configuration
        ConfigureWebHost(webHostBuilder);
      });

    _host = kestrelBuilder.Build();
    _host.Start();

    return testHost;
  }

  /// <summary>
  /// Gets the URL of the Kestrel server for Playwright browser testing
  /// </summary>
  public string GetServerUrl()
  {
    if (_host == null)
      throw new InvalidOperationException("Host has not been created yet");

    var server = _host.Services.GetRequiredService<IServer>();
    var addresses = server.Features.Get<IServerAddressesFeature>();
    return addresses?.Addresses.FirstOrDefault() ?? "http://localhost:5000";
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing)
    {
      _host?.Dispose();
    }
    base.Dispose(disposing);
  }
}

/// <summary>
/// Mock authentication scheme provider that returns the test authentication scheme
/// </summary>
public class MockAuthenticationSchemeProvider : IAuthenticationSchemeProvider
{
  private readonly AuthenticationScheme _scheme;

  public MockAuthenticationSchemeProvider()
  {
    _scheme = new AuthenticationScheme(
      MockAuthenticationHandler.AuthenticationScheme,
      MockAuthenticationHandler.AuthenticationScheme,
      typeof(MockAuthenticationHandler));
  }

  public Task<AuthenticationScheme?> GetSchemeAsync(string name)
  {
    return Task.FromResult(name == MockAuthenticationHandler.AuthenticationScheme ? _scheme : null);
  }

  public Task<AuthenticationScheme?> GetDefaultAuthenticateSchemeAsync()
  {
    return Task.FromResult<AuthenticationScheme?>(_scheme);
  }

  public Task<AuthenticationScheme?> GetDefaultChallengeSchemeAsync()
  {
    return Task.FromResult<AuthenticationScheme?>(_scheme);
  }

  public Task<AuthenticationScheme?> GetDefaultForbidSchemeAsync()
  {
    return Task.FromResult<AuthenticationScheme?>(_scheme);
  }

  public Task<AuthenticationScheme?> GetDefaultSignInSchemeAsync()
  {
    return Task.FromResult<AuthenticationScheme?>(_scheme);
  }

  public Task<AuthenticationScheme?> GetDefaultSignOutSchemeAsync()
  {
    return Task.FromResult<AuthenticationScheme?>(_scheme);
  }

  public Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
  {
    return Task.FromResult<IEnumerable<AuthenticationScheme>>([_scheme]);
  }

  public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
  {
    return Task.FromResult<IEnumerable<AuthenticationScheme>>([]);
  }

  public void AddScheme(AuthenticationScheme scheme)
  {
    // Not needed for testing
  }

  public bool TryAddScheme(AuthenticationScheme scheme)
  {
    // Not needed for testing
    return false;
  }

  public void RemoveScheme(string name)
  {
    // Not needed for testing
  }
}
