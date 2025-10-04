namespace Budget.Api.Features.Authentication;

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresUtc);
