using Budget.Shared.Services;

namespace Budget.Client.Services;

/// <summary>
/// Frontend implementation that loads user data via API calls
/// </summary>
public sealed class ApiUserAndOptionsDataProvider(IUserOptionsApiClient apiClient, ILogger<ApiUserAndOptionsDataProvider> logger)
  : IUserAndOptionsDataProvider
{
  public async Task<UserDetailDto?> LoadUserByIdAsync(int id, CancellationToken cancellationToken = default)
  {
    // log the stacktrace for debugging
    logger.LogDebug("UsersAndOptions:ApiDataProvider:StackTrace: {StackTrace}", Environment.StackTrace);
    // Add logging for potential issues
    logger.LogDebug("UsersAndOptions:ApiDataProvider:Attempting to load user by id: {UserId}", id);
    var rslt = await apiClient.GetUserByIdAsync(id, cancellationToken);
    if (rslt is null)
    {
      logger.LogDebug("UsersAndOptions:ApiDataProvider:User with id {UserId} not found.", id);
      return null;
    }
    else
    {
      logger.LogDebug("UsersAndOptions:ApiDataProvider:User with UserId {Userid} loaded successfully. User ID: {UserId}", id, rslt.Id);
    }
    return rslt;
  }

  public async Task<UserOptions?> LoadUserOptionsAsync(int userId, CancellationToken cancellationToken = default)
  {logger.LogDebug("UsersAndOptions:ApiDataProvider:Loading options for User ID: {UserId}", userId);
    var rslt =await apiClient.GetUserOptionsAsync(userId, cancellationToken);
    logger.LogDebug("UsersAndOptions:ApiDataProvider:Options for User ID: {UserId} loaded successfully.", userId);
  return rslt;
  }

  public async Task<bool> SaveUserOptionsAsync(int userId, UserOptions options,
    CancellationToken cancellationToken = default)
  {
    return await apiClient.SaveUserOptionsAsync(userId, options, cancellationToken);
  }
}