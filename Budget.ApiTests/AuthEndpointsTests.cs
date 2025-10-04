using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Budget.Api.Features.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Budget.ApiTests;

public class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Register_Then_Login_Returns_Token()
    {
        var client = _factory.CreateClient();
        var email = $"user{Guid.NewGuid():N}@test.local";
        var register = new RegisterRequest(email, "P@ssw0rd123!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", register);
        if (regResp.StatusCode != System.Net.HttpStatusCode.Created)
        {
            var body = await regResp.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Registration failed. Status: {regResp.StatusCode}\nBody: {body}");
        }

        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "P@ssw0rd123!"));
        if (loginResp.StatusCode != System.Net.HttpStatusCode.OK)
        {
            var body = await loginResp.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Login failed. Status: {loginResp.StatusCode}\nBody: {body}");
        }
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }
}
