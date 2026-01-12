namespace Budget.ApiTests;

public class AuthEndpointsTests : IntegrationTestBase
{

  [Fact]
  public async Task Register_Then_Login_Returns_Token()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Budget.Api.ApiIdentityContext>();
    var email = $"user{Guid.NewGuid():N}@test.local";
    var register = new RegisterRequest(email, "P@ssw0rd123!");

    // Act - Register
    var regResp = await Client.PostAsJsonAsync("/api/auth/register", register);
    if (regResp.StatusCode != System.Net.HttpStatusCode.Created)
    {
      var body = await regResp.Content.ReadAsStringAsync();
      throw new Xunit.Sdk.XunitException($"Registration failed. Status: {regResp.StatusCode}\nBody: {body}");
    }

    // Act - Login
    var loginResp = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "P@ssw0rd123!"));
    if (loginResp.StatusCode != System.Net.HttpStatusCode.OK)
    {
      var body = await loginResp.Content.ReadAsStringAsync();
      throw new Xunit.Sdk.XunitException($"Login failed. Status: {loginResp.StatusCode}\nBody: {body}");
    }

    // Assert
    var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
    auth.Should().NotBeNull();
    auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
  }
}
