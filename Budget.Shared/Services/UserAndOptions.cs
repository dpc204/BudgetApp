using Budget.Shared.Models;

namespace Budget.Shared.Services
{
  public class UserAndOptions : IUserAndOptions
  {
    public bool HasInfo { get; set; }
    public UserInfoDto  User { get; set; }

    public void SetUserInfo(UserInfoDto userInfo)
    {
      User = userInfo;
      HasInfo = true;
    }

    public void ClearUserInfo()
    {
      User = new UserInfoDto();
      HasInfo = false;
    }

    public bool IsAdminUser()
    {
      return HasInfo && User.Roles.Contains("Admin");
    }
  }


}
