using System.Security.Claims;
using Budget.Shared.Models;
using Budget.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Budget.Web.Components.Auth;

public sealed partial class AuthStateSync : ComponentBase, IDisposable
{
  [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

  [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
  [Inject] private IUserAndOptions UserAndOptions { get; set; } = null!;
  [Inject] private EnvelopeState EnvelopeState { get; set; } = null!;
  private bool _initialized;

  protected override async Task OnInitializedAsync()
  {
    // Seed on first render for cookie-authenticated users
    await SeedFromCurrentStateAsync();

    // Subscribe to changes
    AuthStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
    _initialized = true;
  }

  private async void OnAuthStateChanged(Task<AuthenticationState> task)
  {
    try
    {
      var state = await task;
      await ApplyStateAsync(state);
    }
    catch
    {
      // Intentionally swallow; this runs on a fire-and-forget context
    }
  }

  private async Task SeedFromCurrentStateAsync()
  {
    var state = await (AuthenticationStateTask ?? AuthStateProvider.GetAuthenticationStateAsync());
    await ApplyStateAsync(state);
  }

  private async Task<Task> ApplyStateAsync(AuthenticationState state)
  {


    var user = state.User;
    if (user?.Identity?.IsAuthenticated == true)
    {
      var dto = MapToDto(user);
      UserAndOptions.SetUserInfo(dto);
    }
    else
    {
      UserAndOptions.ClearUserInfo();
    }

    await EnvelopeState.RefreshAsync();

    return Task.CompletedTask;
  }

  private static UserInfoDto MapToDto(ClaimsPrincipal user)
  {
    var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
             ?? user.FindFirstValue("sub")
             ?? user.Identity?.Name;
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

  public void Dispose()
  {
    if (_initialized)
    {
      AuthStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
    }
  }
}