using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Budget.Client.Services;

namespace Budget.Web.Components.Auth;

/// <summary>
/// Synchronizes authentication state with UserAndOptions service.
/// User options are loaded lazily when first accessed - no manual loading needed.
/// </summary>
public sealed partial class AuthStateSync : ComponentBase
{
  [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

  [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
  [Inject] private IUserAndOptions UserAndOptions { get; set; } = null!;

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (!firstRender)
      return;

    var state = await (AuthenticationStateTask ?? AuthStateProvider.GetAuthenticationStateAsync());
    await ApplyOnceAsync(state);
  }

  private Task ApplyOnceAsync(AuthenticationState state)
  {
    var user = state.User;
    var isAuth = user?.Identity?.IsAuthenticated == true;

    if (isAuth)
    {
      // Set user info once - options will load lazily when accessed
      if (!UserAndOptions.HasInfo)
      { 
        var dto = MapToDto(user!);
        UserAndOptions.SetUserInfo(dto);
        
        // That's it! Options load automatically when properties are accessed.
        // No manual loading, no API calls here - just set user info.
      }
    }
    else
    {
      // Clear user info on logout
      if (UserAndOptions.HasInfo)
      {
        UserAndOptions.ClearUserInfo();
      }
    }
    
    return Task.CompletedTask;
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