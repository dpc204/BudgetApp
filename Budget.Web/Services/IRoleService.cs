using Budget.DB;

namespace Budget.Web.Services;

/// <summary>
/// Service contract for managing user roles
/// </summary>
public interface IRoleService
{
  /// <summary>
  /// Gets all available roles
  /// </summary>
  Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Gets roles assigned to a specific user by user ID
  /// </summary>
  Task<List<Role>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Gets roles assigned to a user by email address
  /// </summary>
  Task<List<Role>> GetUserRolesByEmailAsync(string email, CancellationToken cancellationToken = default);

  /// <summary>
  /// Assigns a role to a user
  /// </summary>
  Task<bool> AssignRoleToUserAsync(int userId, int roleId, int? assignedByUserId = null, CancellationToken cancellationToken = default);

  /// <summary>
  /// Removes a role from a user
  /// </summary>
  Task<bool> RemoveRoleFromUserAsync(int userId, int roleId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Checks if a user has a specific role
  /// </summary>
  Task<bool> UserHasRoleAsync(int userId, string roleName, CancellationToken cancellationToken = default);

  /// <summary>
  /// Gets a user by email address (for role assignment)
  /// </summary>
  Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
}
