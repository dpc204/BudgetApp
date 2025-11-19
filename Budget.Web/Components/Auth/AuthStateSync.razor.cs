using System.Security.Claims;
using Microsoft.AspNetCore.Components;

namespace Budget.Web.Components.Auth;

public sealed partial class AuthStateSync : ComponentBase
{
  [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

  [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
  [Inject] private IUserAndOptions UserAndOptions { get; set; } = null!;
  [Inject] private EnvelopeState EnvelopeState { get; set; } = null!;

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (!firstRender)
      return;

    var state = await (AuthenticationStateTask ?? AuthStateProvider.GetAuthenticationStateAsync());
    await ApplyOnceAsync(state);
  }

  private async Task ApplyOnceAsync(AuthenticationState state)
  {
    var user = state.User;
    var isAuth = user?.Identity?.IsAuthenticated == true;

    if (isAuth)
    {
      // Only set once per load
      if (!UserAndOptions.HasInfo)
      {
        var dto = MapToDto(user!);
        UserAndOptions.SetUserInfo(dto);
        await EnvelopeState.RefreshAsync();
      }
    }
    else
    {
      // Only clear once per load
      if (UserAndOptions.HasInfo)
      {
        UserAndOptions.ClearUserInfo();
        // No refresh on logout here to keep it minimal; page reload typically follows logout
      }
    }
  }

  private static string? GetStableUserId(ClaimsPrincipal user)
    => user.FindFirstValue(ClaimTypes.NameIdentifier)
       ?? user.FindFirstValue("sub")
       ?? user.Identity?.Name;

  private static UserInfoDto MapToDto(ClaimsPrincipal user)
  {
    var id = GetStableUserId(user);
    var email = user.FindFirstValue(ClaimTypes.Email);
    var name = user.Identity?.Name
               ?? user.FindFirst("preferred_username")?.Value
               ?? email
               ?? id
               ?? string.Empty;
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    return new UserInfoDto
    {
      Id = id,
      Email = email,
      Name = name,
      Roles = roles
    };
  }
}