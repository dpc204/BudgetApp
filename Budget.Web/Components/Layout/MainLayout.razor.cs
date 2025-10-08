using Microsoft.AspNetCore.Components;

namespace Budget.Web.Components.Layout;

public partial class MainLayout
{
    private bool _drawerOpen = true;

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
    }
}
