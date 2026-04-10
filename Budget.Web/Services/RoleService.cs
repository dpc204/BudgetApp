using Budget.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Budget.Web.Services;

/// <summary>
/// Service for managing user roles in the database
/// </summary>
public class RoleService(BudgetContext context, ILogger<RoleService> logger, HybridCache hybridCache) : IRoleService
{
  public async Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
  {
    return await context.Roles
      .OrderBy(r => r.Name)
      .ToListAsync(cancellationToken);
  }

  public async Task<List<Role>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
  {
    return await context.UserRoles
      .Where(ur => ur.UserId == userId)
      .Select(ur => ur.Role)
      .OrderBy(r => r.Name)
      .ToListAsync(cancellationToken);
  }

  public async Task<List<Role>> GetUserRolesByEmailAsync(string email, CancellationToken cancellationToken = default)
  {
    var user = await GetUserByEmailAsync(email, cancellationToken);
    if(user == null)
    {
      logger.LogWarning("User not found with email: {Email}", email);
      return [];
    }

    return await GetUserRolesAsync(user.Id, cancellationToken);
  }

  public async Task<bool> AssignRoleToUserAsync(
    int userId,
    int roleId,
    int? assignedByUserId = null,
    CancellationToken cancellationToken = default)
  {
    // Check if assignment already exists
    var exists = await context.UserRoles
      .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

    if(exists)
    {
      logger.LogInformation("User {UserId} already has role {RoleId}", userId, roleId);
      return false;
    }

    var userRole = new UserRole {
      UserId = userId,
      RoleId = roleId,
      AssignedAt = DateTime.UtcNow,
      AssignedByUserId = assignedByUserId
    };

    context.UserRoles.Add(userRole);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation(
      "Assigned role {RoleId} to user {UserId} by {AssignedBy}",
      roleId,
      userId,
      assignedByUserId?.ToString() ?? "system");

    return true;
  }

  public async Task<bool> RemoveRoleFromUserAsync(
    int userId,
    int roleId,
    CancellationToken cancellationToken = default)
  {
    var userRole = await context.UserRoles
      .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

    if(userRole == null)
    {
      logger.LogWarning("UserRole not found for user {UserId} and role {RoleId}", userId, roleId);
      return false;
    }

    context.UserRoles.Remove(userRole);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Removed role {RoleId} from user {UserId}", roleId, userId);
    return true;
  }

  public async Task<bool> UserHasRoleAsync(
    int userId,
    string roleName,
    CancellationToken cancellationToken = default)
  {
    return await context.UserRoles
      .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName, cancellationToken);
  }

  public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
  {
    var inEmail = email.ToUpper();

    return await hybridCache.GetOrCreateAsync<User?>($"UserByEmail:{email}", async ct =>
    {
      return await context.Users
        .IgnoreQueryFilters()
        .TagWithCallSite()
        .FirstOrDefaultAsync(u => u.Email == inEmail, ct);
    }, cancellationToken: cancellationToken);
  }
}