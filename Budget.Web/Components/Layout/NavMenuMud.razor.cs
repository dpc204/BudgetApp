using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace Budget.Web.Components.Layout;

public partial class NavMenuMud
{
  [Inject]
  private NavigationManager NavigationManager { get; set; } = default!;





  private void HandleLogoutAsync()
  {
    var currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
    NavigationManager.NavigateTo($"/Account/Logout?ReturnUrl={Uri.EscapeDataString(currentUrl)}", forceLoad: true);
  }

  /// <summary>
  /// Gets the highest role for the current user from Entra claims
  /// </summary>
  private static string? GetUserRole(ClaimsPrincipal user)
  {
    if(user.IsInRole("Admin"))
      return "Admin";
    if(user.IsInRole("PowerUser"))
      return "PowerUser";
    if(user.IsInRole("User"))
      return "User";
    return null;
  }

  /// <summary>
  /// Gets the MudBlazor color for a role badge
  /// </summary>
  private static Color GetRoleColor(string role)
  {
    return role switch {
      "Admin" => Color.Error,
      "PowerUser" => Color.Warning,
      "User" => Color.Info,
      _ => Color.Default
    };
  }
}
