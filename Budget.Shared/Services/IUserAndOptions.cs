namespace Budget.Shared.Services
{
  public interface IUserAndOptions
  {
    bool HasInfo { get; set; }
    UserInfoDto User { get; set; }
    UserOptions Options { get; set; }
    void SetUserInfo(UserInfoDto userInfo);
    void ClearUserInfo();
    bool IsAdminUser();
  }
}
