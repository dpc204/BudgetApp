namespace Budget.Shared.Services
{
  public interface IUserAndOptions
  {
    bool HasInfo { get; set; }
    UserInfoDto User { get; set; }

    /// <summary>
    /// Returns the current user snapshot and may start lazy loading in the background.
    /// </summary>
    UserInfoDto GetUser();

    Task<UserInfoDto> GetUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user options synchronously. May return default values if not yet loaded.
    /// RECOMMENDED: Use GetOptionsAsync() or EnsureOptionsLoadedAsync() to ensure loaded options.
    /// </summary>
    UserOptions Options { get; set; }

    /// <summary>
    /// Event fired when options finish loading from the API.
    /// </summary>
    event Action? OptionsLoaded;

    /// <summary>
    /// Sets the user's email for lazy loading user info from database
    /// </summary>
    void SetUserEmail(string email);

    /// <summary>
    /// Sets the user's ID and FamilyId directly (used when passed from headers)
    /// </summary>
    void SetUserIdAndFamilyId(int userId, int familyId);

    Task SetupAsync(CancellationToken ct);
    void SetUserInfo(UserInfoDto userInfo);
    void ClearUserInfo();
    bool IsAdminUser();

    ///// <summary>
    ///// Gets user options, loading from API if needed. Safe to call multiple times.
    ///// This is the RECOMMENDED way to access options - ensures they're loaded.
    ///// </summary>
    //ValueTask<UserOptions> GetOptionsAsync();

    /// <summary>
    /// Ensures user options are loaded from the API. Safe to call multiple times.
    /// Alternative to GetOptionsAsync() with same behavior.
    /// </summary>
    Task EnsureOptionsLoadedAsync();
  }
}
