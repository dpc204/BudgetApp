using System.Security.Claims;
using Budget.Web.Services;
using Microsoft.AspNetCore.Authentication;

namespace Budget.Web.Authorization;

/// <summary>
/// Transforms claims after Entra ID authentication to add database roles
/// This runs automatically after successful authentication and adds role claims
/// from the database to the user's ClaimsPrincipal
/// </summary>
public class DatabaseClaimsTransformation(
  IRoleService roleService,
  ILogger<DatabaseClaimsTransformation> logger) : IClaimsTransformation
{
  public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
  {
    // Only transform if user is authenticated
    if (principal.Identity?.IsAuthenticated != true)
    {
      return principal;
    }

    // Check if we've already added database roles (avoid duplicate transformation)
    if (principal.HasClaim(c => c.Type == "database_roles_loaded"))
    {
      return principal;
    }

    // IMPORTANT: Skip transformation if this is a token cache refresh scenario
    // The user_null error occurs when MSAL tries to silently acquire a token
    // but doesn't have enough account information in the cache yet
    // We'll let the user complete authentication first, then load roles on subsequent requests
    var hasAccountInfo = principal.HasClaim(c => c.Type == "oid" || c.Type == "sub");
    if (!hasAccountInfo)
    {
      logger.LogDebug("Skipping role transformation - no account identifier claims found yet");
      return principal;
    }

    // Get user email from Entra ID claims
    var email = principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst("preferred_username")?.Value
                ?? principal.FindFirst("upn")?.Value;

    if (string.IsNullOrEmpty(email))
    {
      logger.LogWarning("No email claim found for user {User}", principal.Identity.Name ?? "unknown");
      return principal;
    }

    logger.LogInformation("Loading database roles for user {Email}", email);

    try
    {
      // Load roles from database
      var roles = await roleService.GetUserRolesByEmailAsync(email);

      if (roles.Count == 0)
      {
        logger.LogWarning("No roles found in database for user {Email}", email);
        return principal;
      }

      // Create new identity with database roles
      var claimsIdentity = new ClaimsIdentity();

      // Add role claims
      foreach (var role in roles)
      {
        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
        logger.LogInformation("Added role {Role} for user {Email}", role.Name, email);
      }

      // Add marker claim to prevent duplicate transformation
      claimsIdentity.AddClaim(new Claim("database_roles_loaded", "true"));

      // Add claims to principal
      principal.AddIdentity(claimsIdentity);

      logger.LogInformation(
        "Successfully loaded {Count} database roles for user {Email}",
        roles.Count,
        email);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error loading database roles for user {Email}", email);
      // Don't throw - allow authentication to succeed even if role loading fails
    }

    return principal;
  }
}
