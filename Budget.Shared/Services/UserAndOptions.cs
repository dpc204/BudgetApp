using Microsoft.Extensions.Validation;

namespace Budget.Shared.Services;

public class UserAndOptions : IUserAndOptions
{
  private readonly IUserAndOptionsDataProvider? _dataProvider;
  private Task<UserOptions>? _loadOptionsTask;
  private bool _optionsLoadAttempted;
  private Task<UserInfoDto>? _loadUserTask;
  private bool _userLoadAttempted;

  /// <summary>
  /// Event fired when options finish loading from the API.
  /// Subscribe to this in components to get notified when lazy-loaded options are ready.
  /// </summary>
  public event Action? OptionsLoaded;

  public UserAndOptions()
  {
    _options = new UserOptions() { UserAndOptions = this, UserId = -1 };
    // Parameterless constructor for cases where data provider is not available
  }

  public UserAndOptions(IUserAndOptionsDataProvider dataProvider, ILogger<UserAndOptions> logger)
  {
    _options = new UserOptions() { UserAndOptions = this, UserId = -2 };
    _dataProvider = dataProvider;
    WireUpOptionsChangeHandler();
  }

  public bool HasInfo { get; set; }
  private bool _hasUserInfo = false;
  private UserInfoDto _user = new UserInfoDto();

  /// <summary>
  /// Snapshot of the current user info. Does not trigger lazy loading.
  /// Use GetUser or GetUserAsync for lazy-loading behavior.
  /// </summary>
  public UserInfoDto User
  {
    get { return _user; }
    set => _user = value;
  }

  /// <summary>
  /// Returns the current user snapshot and kicks off lazy loading in the background if needed.
  /// Does not block the caller.
  /// </summary>
  public UserInfoDto GetUser()
  {
    if (!_hasUserInfo && !_userLoadAttempted && _dataProvider != null && !string.IsNullOrWhiteSpace(_userEmail))
    {
      _userLoadAttempted = true;
      _loadUserTask = LoadUserInternalAsync();
      _ = _loadUserTask; // fire-and-forget
    }

    return User;
  }

  public async Task SetupAsync(CancellationToken ct)
  {
    await GetUserAsync(ct);
    await LoadOptionsInternalAsync();
  }

  /// <summary>
  /// Ensures the user is loaded and returns the up-to-date user info.
  /// Safe to await from Blazor lifecycle methods.
  /// </summary>
  public async Task<UserInfoDto> GetUserAsync(CancellationToken cancellationToken = default)
  {
    if (User.Id <= 0)
      return User;

    if (_userLoadAttempted && _loadUserTask != null)
    {
      var rslt = await _loadUserTask.ConfigureAwait(false);
      return rslt ?? User;
    }

    if (!_userLoadAttempted && _dataProvider != null)
    {
      _userLoadAttempted = true;
      _loadUserTask = LoadUserInternalAsync(cancellationToken);
      var rslt = await _loadUserTask.ConfigureAwait(false);
      return rslt ?? User;
    }


    await LoadOptionsInternalAsync();

    return User;
  }

  string _userEmail = string.Empty;

  public void SetUserEmail(string email)
  {
    _userEmail = email;
    // Don't load here - let it happen lazily later
  }

  public void SetUserIdAndFamilyId(int userId, int familyId)
  {
    // Set user info directly from headers - no database lookup needed
    User = new UserInfoDto
    {
      Id = userId,
      FamilyId = familyId,
      Email = _userEmail,
      Name = string.Empty, // Will be populated if needed
      Roles = []
    };
    _hasUserInfo = userId > 0;
    HasInfo = _hasUserInfo;
  }

  public void SetUserInfo(UserInfoDto userInfo)
  {
    User = userInfo;
    HasInfo = true;
  }


  /// <summary>
  /// Ensures user options are loaded. Safe to call multiple times - only loads once.
  /// This is the preferred way to access options - it ensures they're loaded before returning.
  /// </summary>
  public async Task EnsureOptionsLoadedAsync()
  {
    // Already loaded or load in progress
    if (_optionsLoadAttempted && _loadOptionsTask != null)
    {
      return;
    }

    // First call - start loading
    if (!_optionsLoadAttempted && _dataProvider != null && User.Id != 0)
    {
      _optionsLoadAttempted = true;
      _loadOptionsTask = LoadOptionsInternalAsync();
      var rslt = await _loadOptionsTask;
    }

    // No data provider or not authenticated - return current options
    return; //Options;
  }

  ///// <summary>
  ///// Gets user options, automatically loading from API if needed.
  ///// RECOMMENDED: Use EnsureOptionsLoadedAsync() instead to await the load properly.
  ///// This property returns default options if not yet loaded.
  ///// </summary>
  //public async ValueTask<UserOptions> GetOptionsAsync()
  //{
  //  return await EnsureOptionsLoadedAsync();
  //}

  private async Task<UserOptions> LoadOptionsInternalAsync()
  {
    try
    {
      if (HasInfo)
        return Options;

      Loading = true;
      var loaded = await _dataProvider!.LoadUserOptionsAsync(User.Id);
      if (loaded != null)
      {
        Options = loaded;
      }

      // Notify subscribers that options are loaded
      OptionsLoaded?.Invoke();
      HasInfo = true;
      return Options;
    }
    catch (Exception)
    {
      // Return default options on error
      return Options;
    }
    finally
    {
      Loading = false;
    }
  }

  public bool Loading { get; set; }


  private async Task<UserInfoDto?> LoadUserInternalAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      var response = await _dataProvider!.LoadUserByIdAsync(_user.Id, cancellationToken);

      if (response is null)
        return null;

      var name = string.Join(' ', response.FirstName, response.LastName);

      var rslt = new UserInfoDto
      {
        Id = response.Id,
        Email = response.Email,
        Name = name,
        FamilyId = response.FamilyId,
        Roles = response.Roles.Select(a => a.Name).ToArray()
      };
      _hasUserInfo = rslt.Id != 0;

      // update snapshot so subsequent GetUser() calls see the loaded data
      User = rslt;

      return rslt;
    }
    catch (Exception)
    {
      // Return default options on error
      return new UserInfoDto();
    }
  }

  public void ClearUserInfo()
  {
    User = new UserInfoDto();

    // Unwire old handlers before creating new Options
    if (Options != null)
    {
      Options.PropertyChanged -= OnOptionsChanged;
      Options.PropertyRead -= OnOptionsPropertyRead;
    }

    Options = new UserOptions();
    WireUpOptionsChangeHandler();
    HasInfo = false;

    // Reset load tracking
    _optionsLoadAttempted = false;
    _loadOptionsTask = null;
  }

  public bool IsAdminUser()
  {
    return HasInfo && User.Roles.Contains("Admin");
  }

  private UserOptions _options = new();

  /// <summary>
  /// Gets or sets user options. 
  /// Properties automatically trigger lazy loading when accessed - no manual initialization needed!
  /// </summary>
  public UserOptions Options
  {
    get
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
      EnsureOptionsLoadedAsync();
#pragma warning restore CS4014
      return _options;
    }
    set
    {
      _options = value;
      WireUpOptionsChangeHandler();
      _options.UserAndOptions = this;
    }
  }

  private void WireUpOptionsChangeHandler()
  {
    if (_options != null)
    {
      _options.PropertyChanged -= OnOptionsChanged; // Prevent duplicate subscriptions
      _options.PropertyChanged += OnOptionsChanged;

      // Wire up lazy load trigger on property read
      _options.PropertyRead -= OnOptionsPropertyRead;
      _options.PropertyRead += OnOptionsPropertyRead;
    }
  }

  private async Task OnOptionsPropertyRead()
  {
    // When any property is read, ensure options are loaded
    // This makes lazy loading completely transparent
    if (!_optionsLoadAttempted && _dataProvider != null && HasInfo && User.Id != 0)
    {
      _optionsLoadAttempted = true;
      _loadOptionsTask = LoadOptionsInternalAsync();
      await _loadOptionsTask;
    }
  }

  private async void OnOptionsChanged()
  {
    if (_dataProvider != null && User.Id != 0 && !Loading)
    {
      try
      {
        // Ensure UserId is set
        if (Options.UserId == 0)
        {
          Options.UserId = User.Id;
        }

        await _dataProvider.SaveUserOptionsAsync(User.Id, Options);
      }
      catch
      {
        // Silently fail - options will be saved on next change or at next login
        // Could add logging here if needed
      }
    }
  }
}