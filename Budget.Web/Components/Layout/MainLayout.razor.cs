using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Budget.Web.Components.Layout;

public partial class MainLayout : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private bool _drawerOpen = true;
    private IJSObjectReference? _module;
    private bool _jsInitialized;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_jsInitialized)
        {
            try
            {
                _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/drawer.js");
                await _module.InvokeVoidAsync("initialize");
                _drawerOpen = await _module.InvokeAsync<bool>("eval", "window.mudDrawer.isOpen()");
                _jsInitialized = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing drawer: {ex.Message}");
            }
        }
    }

    private async Task ToggleDrawer()
    {
        if (_module != null && _jsInitialized)
        {
            try
            {
                _drawerOpen = await _module.InvokeAsync<bool>("eval", "window.mudDrawer.toggle()");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling drawer: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
