using Microsoft.AspNetCore.Authorization;

namespace Budget.Web.Authorization;

/// <summary>
/// Authorization requirement for database-stored roles
/// </summary>
public class DatabaseRoleRequirement(params string[] allowedRoles) : IAuthorizationRequirement
{
  /// <summary>
  /// List of role names that satisfy this requirement
  /// </summary>
  public IReadOnlyList<string> AllowedRoles { get; } = allowedRoles;
}
