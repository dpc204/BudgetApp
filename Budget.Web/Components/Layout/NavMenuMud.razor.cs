using Microsoft.AspNetCore.Components;

namespace Budget.Web.Components.Layout;

public partial class NavMenuMud
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private void HandleLogoutAsync()
    {
        var currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.NavigateTo($"Account/Logout?ReturnUrl={Uri.EscapeDataString(currentUrl)}", forceLoad: true);
    }
}
