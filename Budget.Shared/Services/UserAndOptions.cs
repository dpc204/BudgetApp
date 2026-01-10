namespace Budget.Shared.Services;

public class UserAndOptions : IUserAndOptions
{
  private readonly IBudgetApiClient? _apiClient;

  public UserAndOptions()
  {
    // Parameterless constructor for cases where API client is not available
  }

  public UserAndOptions(IBudgetApiClient apiClient)
  {
    _apiClient = apiClient;
    WireUpOptionsChangeHandler();
  }

  public bool HasInfo { get; set; }
  public UserInfoDto User { get; set; } = new UserInfoDto();

  public void SetUserInfo(UserInfoDto userInfo)
  {
    User = userInfo;
    HasInfo = true;
    // Update UserId in Options when user info is set
    if (Options != null && !string.IsNullOrEmpty(userInfo.Id))
    {
      Options.UserId = userInfo.Id;
    }
  }

  public void ClearUserInfo()
  {
    User = new UserInfoDto();
    
    // Unwire old handler before creating new Options
    if (Options != null)
    {
      Options.PropertyChanged -= OnOptionsChanged;
    }
    
    Options = new UserOptions();
    WireUpOptionsChangeHandler();
    HasInfo = false;
  }

  public bool IsAdminUser()
  {
    return HasInfo && User.Roles.Contains("Admin");
  }

  private UserOptions _options = new();
  public UserOptions Options
  {
    get => _options;
    set
    {
      // Unwire old handler
      if (_options != null)
      {
        _options.PropertyChanged -= OnOptionsChanged;
      }
      
      _options = value;
      WireUpOptionsChangeHandler();
    }
  }

  private void WireUpOptionsChangeHandler()
  {
    if (_options != null)
    {
      _options.PropertyChanged -= OnOptionsChanged; // Prevent duplicate subscriptions
      _options.PropertyChanged += OnOptionsChanged;
    }
  }

  private async void OnOptionsChanged()
  {
    if (_apiClient != null && HasInfo && !string.IsNullOrEmpty(User.Id))
    {
      try
      {
        // Ensure UserId is set
        if (string.IsNullOrEmpty(Options.UserId))
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
