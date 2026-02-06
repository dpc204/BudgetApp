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
    logger.LogDebug("DatabaseClaimsTransformation.TransformAsync called");

    // Only transform if user is authenticated
    if (principal.Identity?.IsAuthenticated != true)
    {
      logger.LogDebug("Skipping transformation - user not authenticated");
      return principal;
    }

    // Check if we've already added database roles (avoid duplicate transformation)
    if (principal.HasClaim(c => c.Type == "database_roles_loaded"))
    {
      logger.LogDebug("Skipping transformation - database_roles_loaded claim already present");
      return principal;
    }

    // Get user email from Entra ID claims - this is what we actually need to load from database
    var email = principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst("preferred_username")?.Value
                ?? principal.FindFirst("upn")?.Value;

    // IMPORTANT: Skip transformation only if we have neither email nor account identifiers
    // The user_null error occurs when MSAL tries to silently acquire a token
    // but doesn't have enough account information in the cache yet
    var hasAccountInfo = principal.HasClaim(c => c.Type == "oid" || c.Type == "sub");

    if (string.IsNullOrEmpty(email) && !hasAccountInfo)
    {
      logger.LogDebug("Skipping role transformation - no email or account identifier claims found yet");
      return principal;
    }

    if (string.IsNullOrEmpty(email))
    {
      logger.LogWarning("No email claim found for user {User}. Available claims: {Claims}", 
        principal.Identity.Name ?? "unknown",
        string.Join(", ", principal.Claims.Select(c => $"{c.Type}={c.Value}")));
      return principal;
    }

    logger.LogInformation("Loading database roles for user {Email}", email);

    try
    {
      // Get user from database to access FamilyId
      var user = await roleService.GetUserByEmailAsync(email);

      if (user == null)
      {
        logger.LogWarning("User not found in database for email {Email}", email);
        return principal;
      }

      // Load roles from database
      var roles = await roleService.GetUserRolesAsync(user.Id);

      if (roles.Count == 0)
      {
        logger.LogWarning("No roles found in database for user {Email}", email);
      }

      // Create new identity with database roles, UserId, and FamilyId
      var claimsIdentity = new ClaimsIdentity();

      // Add UserId claim (needed for UserAndOptions)
      claimsIdentity.AddClaim(new Claim("UserId", user.Id.ToString()));
      logger.LogInformation("Added UserId claim: {UserId} for user {Email}", user.Id, email);

      // Add FamilyId claim (critical for multi-tenancy)
      claimsIdentity.AddClaim(new Claim("FamilyId", user.FamilyId.ToString()));
      logger.LogInformation("Added FamilyId claim: {FamilyId} for user {Email}", user.FamilyId, email);

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
        "Successfully loaded FamilyId ({FamilyId}) and {Count} database roles for user {Email}",
        user.FamilyId,
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
