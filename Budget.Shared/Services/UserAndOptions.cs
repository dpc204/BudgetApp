namespace Budget.Shared.Services;

public class UserAndOptions : IUserAndOptions
{
  public bool HasInfo { get; set; }
  public UserInfoDto User { get; set; } = new UserInfoDto();

  public void SetUserInfo(UserInfoDto userInfo)
  {
    User = userInfo;
    HasInfo = true;
  }

  public void ClearUserInfo()
  {
    User = new UserInfoDto();
    Options = new UserOptions();
    HasInfo = false;
  }

  public bool IsAdminUser()
  {
    return HasInfo && User.Roles.Contains("Admin");
  }

  public UserOptions Options { get; set; } = new UserOptions();

}
