using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Budget.Client.Pages;

public sealed partial class Auth : ComponentBase
{
    [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private string? UserName { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthenticationStateTask;
        var user = state.User;
        UserName = user.Identity?.Name
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? "(unknown)";
    }
}
