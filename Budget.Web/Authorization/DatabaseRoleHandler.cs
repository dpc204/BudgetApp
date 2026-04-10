using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Budget.Web.Authorization;

/// <summary>
/// Authorization handler that validates database roles from claims
/// </summary>
public class DatabaseRoleHandler(ILogger<DatabaseRoleHandler> logger) : AuthorizationHandler<DatabaseRoleRequirement>
{
  protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    DatabaseRoleRequirement requirement)
  {
    // Get all role claims added by DatabaseClaimsTransformation
    var userRoles = context.User.Claims
      .Where(c => c.Type == ClaimTypes.Role)
      .Select(c => c.Value)
      .ToList();

    logger.LogDebug(
      "Checking authorization for user {User}. User roles: {UserRoles}. Required roles: {RequiredRoles}",
      context.User.Identity?.Name ?? "unknown",
      string.Join(", ", userRoles),
      string.Join(", ", requirement.AllowedRoles));

    // Check if user has any of the allowed roles
    if(userRoles.Any(role => requirement.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
    {
      logger.LogDebug("Authorization succeeded for user {User}", context.User.Identity?.Name ?? "unknown");
      context.Succeed(requirement);
    }
    else
    {
      logger.LogWarning(
        "Authorization failed for user {User}. Required one of: {RequiredRoles}. User has: {UserRoles}",
        context.User.Identity?.Name ?? "unknown",
        string.Join(", ", requirement.AllowedRoles),
        string.Join(", ", userRoles));
    }

    return Task.CompletedTask;
  }
}
