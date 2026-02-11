namespace Budget.Shared.Services;

/// <summary>
/// API client for user options and user profile operations
/// </summary>
public interface IUserOptionsApiClient
{
  Task<UserDetailDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
  Task<UserOptions?> GetUserOptionsAsync(int userId, CancellationToken cancellationToken = default);
  Task<bool> SaveUserOptionsAsync(int userId, UserOptions options, CancellationToken cancellationToken = default);
}
