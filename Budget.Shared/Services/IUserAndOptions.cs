using Budget.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Budget.Shared.Services
{
  public interface IUserAndOptions
  {
    bool HasInfo { get; set; }
    UserInfoDto User { get; set; }
    void SetUserInfo(UserInfoDto userInfo);
    void ClearUserInfo();
    bool IsAdminUser();
  }
}
