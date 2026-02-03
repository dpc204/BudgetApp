using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Budget.Client.Services;
using Budget.DB;

namespace Budget.Web.Components.Auth;

public sealed partial class AuthStateSync : ComponentBase
{
  [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

  [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
  [Inject] private IUserAndOptions UserAndOptions { get; set; } = null!;
  [Inject] private EnvelopeState EnvelopeState { get; set; } = null!;
  [Inject] private IBudgetApiClient ApiClient { get; set; } = null!;
  [Inject] public required BudgetContext db { get; set; } 
  [Inject] public required ILogger<AuthStateSync> logger { get; set; }
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
      // Only set user info once per load
      // Options will be loaded lazily when first accessed via EnsureOptionsLoadedAsync()
      if (!UserAndOptions.HasInfo)
      { 
        var dto = MapToDto(user!);
        UserAndOptions.SetUserInfo(dto);
        
        // Don't load options here - too early in auth pipeline
        // Components that need options should call: await UserAndOptions.EnsureOptionsLoadedAsync()
        
        //try
        //{
        //  await EnvelopeState.RefreshAsync();
        //}
        //catch (Exception ex)
        //{
        //  // Log but don't fail
        //  Console.WriteLine($"Failed to refresh envelope state: {ex.Message}");
        //}
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

  private  UserInfoDto MapToDto(ClaimsPrincipal user)
  {
    var id = GetStableUserId(user);
    var email = user.FindFirstValue(ClaimTypes.Email);
    var name = user.Identity?.Name
               ?? user.FindFirst("preferred_username")?.Value
               ?? email
               ?? id
               ?? string.Empty;
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

    var dbUser = db.Users.FirstOrDefault(a => a.Email.ToUpper() == name!.ToUpper());

    if (dbUser == null)
    {
      logger.LogError("User with email {Email} not found in database.", email);
      throw new InvalidOperationException($"User with email {email}  not found in database.");
    }

    return new UserInfoDto
    {
      Id = dbUser.Id,
      Email = name,
      FamilyId = dbUser.FamilyId,
      Name =   dbUser.FirstName,  
      Roles = roles
    };
  }
}