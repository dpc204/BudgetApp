namespace Budget.Client.Services;

/// <summary>
/// Implementation of user options API client
/// </summary>
public sealed class UserOptionsApiClient(HttpClient http, ILogger<UserOptionsApiClient> logger) : Shared.Services.IUserOptionsApiClient
{
  public async Task<UserDetailDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
  {
    try
    {
      logger.LogDebug("UserAndOptions:Starting GetUserByIdAsync for Id: {Id}", id);
      var url = $"/api/useroptions/GetUserById?Id={id}";
      logger.LogDebug("Request URL: {Url}", url);

      var response = await http.GetFromJsonAsync<GetUserByIdResponse>(url, cancellationToken);

      logger.LogDebug("UserAndOptions:Received response for GetUserByIdAsync: {HasValue}", response?.User != null);
      return response?.User;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "UserAndOptions:Error in GetUserByIdAsync for Id: {Id}", id);
      return null;
    }
  }

  public async Task<UserOptions?> GetUserOptionsAsync(int userId, CancellationToken cancellationToken = default)
  {
    try
    {
      var response =
        await http.GetFromJsonAsync<GetUserOptionsResponse>($"/api/useroptions/{userId}",
          cancellationToken: cancellationToken);
      return response?.Options;
    }
    catch (Exception ex)
    {
      logger.LogDebug(ex, "Failed to get user options for user {UserId}", userId);
      return null;
    }
  }

  public async Task<bool> SaveUserOptionsAsync(int userId, UserOptions options,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var command = new SaveUserOptionsCommand(userId, options);
      using var resp = await http.PostAsJsonAsync("/api/useroptions", command, cancellationToken);
      return resp.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Failed to save user options for user {UserId}", userId);
      return false;
    }
  }

  private sealed record GetUserByIdResponse(UserDetailDto? User);
  private sealed record GetUserOptionsResponse(UserOptions? Options);
  private sealed record SaveUserOptionsCommand(int UserId, UserOptions Options);
}
