using Microsoft.Extensions.Validation;

namespace Budget.Shared.Services;

public class UserAndOptions : IUserAndOptions
{
  private readonly IBudgetApiClient? _apiClient;
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
    _options = new UserOptions() { UserAndOptions = this , UserId = -1};
    // Parameterless constructor for cases where API client is not available
  }

  public UserAndOptions(IBudgetApiClient apiClient)
  {
    _options = new UserOptions() { UserAndOptions = this, UserId = -2 };
    _apiClient = apiClient;
    WireUpOptionsChangeHandler();
  }

  public bool HasInfo { get; set; }
  private  bool _hasUserInfo = false;
  private UserInfoDto _user = new UserInfoDto();

  public UserInfoDto User 
  { 
    get
    {
      if (!_hasUserInfo)
      {
        var usr = EnsureUserLoadedAsync();
        _user = usr.Result ?? new UserInfoDto();
      }

      return _user;

    }
    set => _user = value;
  }

  
  
  private async Task<UserInfoDto?> EnsureUserLoadedAsync()
  {
    if (string.IsNullOrWhiteSpace(_userEmail))
      return User;


    if(_userLoadAttempted && _loadUserTask != null)
    {
      return await _loadUserTask;
    }

    // First call - start loading
    if(!_userLoadAttempted && _apiClient != null )
    {
      _userLoadAttempted = true;
      _loadUserTask = LoadUserInternalAsync();
      _hasUserInfo = true;
      return await _loadUserTask;
    }


    
    return User;

  }

  string _userEmail = string.Empty;
  public void SetUserEmail(string email)
  {
    _userEmail = email;
  }

  public void SetUserInfo(UserInfoDto userInfo)
  {
    User = userInfo;
    HasInfo = true;
    // Update UserId in Options when user info is set
    //if (Options != null && userInfo.Id != 0)
    //{
    //  Options.UserId = userInfo.Id;
    //}
  }
  
  

  /// <summary>
  /// Ensures user options are loaded. Safe to call multiple times - only loads once.
  /// This is the preferred way to access options - it ensures they're loaded before returning.
  /// </summary>
  public async Task<UserOptions> EnsureOptionsLoadedAsync()
  {
    // Already loaded or load in progress
    if (_optionsLoadAttempted && _loadOptionsTask != null)
    {
      return await _loadOptionsTask;
    }
    
    // First call - start loading
    if (!_optionsLoadAttempted && _apiClient != null && HasInfo && User.Id != 0)
    {
      _optionsLoadAttempted = true;
      _loadOptionsTask = LoadOptionsInternalAsync();
      return await _loadOptionsTask;
    }
    
    // No API client or not authenticated - return current options
    return Options;
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
      var loaded = await _apiClient!.GetUserOptionsAsync(User.Id);
      if (loaded != null)
      {
        Options = loaded;
      }
      
      // Notify subscribers that options are loaded
      OptionsLoaded?.Invoke();
      
      return Options;
    }
    catch (Exception)
    {
      // Return default options on error
      return Options;
    }
  }


  private async Task<UserInfoDto?> LoadUserInternalAsync()
  {
    try
    {

      var response = await _apiClient.GetUserByEmailAsync(_userEmail);

      if(response is null)
        return null;

      var name = string.Join(' ', response.FirstName, response.LastName);

      var rslt = new UserInfoDto {
        Id = response.Id,
        Email = response.Email,
        Name = name,
        FamilyId = response.FamilyId,
        Roles = response.Roles.Select(a => a.Name).ToArray()
      };
      _hasUserInfo = rslt.Id != 0;

      return rslt;
    }
    catch(Exception)
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
      EnsureOptionsLoadedAsync();
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
    if (!_optionsLoadAttempted && _apiClient != null && HasInfo && User.Id != 0)
    {
      _optionsLoadAttempted = true;
      _loadOptionsTask = LoadOptionsInternalAsync();
      await _loadOptionsTask;
    }
  }

  private async void OnOptionsChanged()
  {
    if (_apiClient != null && HasInfo && User.Id != 0)
    {
      try
      {
        // Ensure UserId is set
        if (Options.UserId == 0)
        {
          Options.UserId = User.Id;
        }
        
        await _apiClient.SaveUserOptionsAsync(User.Id, Options);
      }
      catch
      {
        // Silently fail - options will be saved on next change or at next login
        // Could add logging here if needed
      }
    }
  }
}
