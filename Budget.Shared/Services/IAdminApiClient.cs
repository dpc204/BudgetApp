namespace Budget.Shared.Services;

/// <summary>
/// API client for admin operations (roles and users)
/// </summary>
public interface IAdminApiClient
{
  // Role management
  Task<IEnumerable<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
  Task<RoleDto?> GetRoleAsync(int id, CancellationToken cancellationToken = default);
  Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
  Task<RoleDto> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
  Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);

  // User management
  Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
  Task<UserDetailDto?> GetUserAsync(int id, CancellationToken cancellationToken = default);
  Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);

  // User-Role management
  Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
  Task<AssignRoleResponse> AssignRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default);
  Task<bool> RemoveRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default);
}
