namespace Budget.Shared.Services;

/// <summary>
/// Abstraction for loading user and options data.
/// Implemented differently in frontend (API calls) and backend (direct DB access).
/// </summary>
public interface IUserAndOptionsDataProvider
{
  /// <summary>
  /// Load user details by email address
  /// </summary>
  Task<UserDetailDto?> LoadUserByIdAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>
  /// Load user options by user ID
  /// </summary>
  Task<UserOptions?> LoadUserOptionsAsync(int userId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Save user options
  /// </summary>
  Task<bool> SaveUserOptionsAsync(int userId, UserOptions options, CancellationToken cancellationToken = default);
}
