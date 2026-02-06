using Budget.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SharedUserOptions = Budget.Shared.Services.UserOptions;

namespace Budget.Api.Services;

/// <summary>
/// Backend implementation that loads user data directly from the database
/// </summary>
public sealed class DbUserAndOptionsDataProvider(BudgetContext db, ILogger<DbUserAndOptionsDataProvider> logger) : IUserAndOptionsDataProvider
{
  public async Task<UserDetailDto?> LoadUserByIdAsync(int id, CancellationToken cancellationToken = default)
  {
  
    var user = await db.Users
      .IgnoreQueryFilters()
      .Where(u => u.Id == id)
      .Select(u => new UserDetailDto(
        u.Id,
        u.Email,
        u.FirstName,
        u.LastName,
        u.FamilyId,
        db.UserRoles
          .Where(ur => ur.UserId == u.Id)
          .Select(ur => new RoleInfoDto(ur.Role.Id, ur.Role.Name))
          .ToList()
      ))
      .FirstOrDefaultAsync(cancellationToken);

    return user;
  }

  public async Task<SharedUserOptions?> LoadUserOptionsAsync(int userId, CancellationToken cancellationToken = default)
  {
    var savedOptions = await db.SavedUserOptions
      .AsNoTracking()
      .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    if (savedOptions == null || string.IsNullOrEmpty(savedOptions.JsonOptions))
    {
      return null;
    }

    try
    {
      var options = JsonSerializer.Deserialize<SharedUserOptions>(savedOptions.JsonOptions);
      return options;
    }
    catch (JsonException ex)
    {
      logger.LogError(ex, "Failed to deserialize user options for user {UserId}", userId);
      return null;
    }
  }

  public async Task<bool> SaveUserOptionsAsync(int userId, SharedUserOptions options, CancellationToken cancellationToken = default)
  {
    try
    {
      var savedOptions = await db.SavedUserOptions
        .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

      var jsonOptions = JsonSerializer.Serialize(options);

      if (savedOptions == null)
      {
        savedOptions = new DB.SavedUserOptions
        {
          UserId = userId,
          JsonOptions = jsonOptions
        };
        db.SavedUserOptions.Add(savedOptions);
      }
      else
      {
        savedOptions.JsonOptions = jsonOptions;
      }

      await db.SaveChangesAsync(cancellationToken);
      return true;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to save user options for user {UserId}", userId);
      return false;
    }
  }
}
