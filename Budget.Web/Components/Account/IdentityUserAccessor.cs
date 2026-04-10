namespace Budget.Web.Components.Account
{
  /// <summary>
  /// Provides access to the current user information from Entra ID claims
  /// </summary>
  internal sealed class IdentityUserAccessor(IdentityRedirectManager redirectManager)
  {
    /// <summary>
    /// Gets the current user information from Entra ID claims
    /// </summary>
    public async Task<BudgetUser> GetRequiredUserAsync(HttpContext context)
    {
      var claimsPrincipal = context.User;

      if(claimsPrincipal?.Identity?.IsAuthenticated != true)
      {
        redirectManager.RedirectToWithStatus("Account/Login", "Error: User is not authenticated.");
        throw new InvalidOperationException("User not authenticated");
      }

      // Extract user information from Entra ID claims
      var user = new BudgetUser {
        Id = GetEntraObjectId(claimsPrincipal) ?? throw new InvalidOperationException("User ID not found in claims"),
        UserName = claimsPrincipal.Identity.Name ?? claimsPrincipal.FindFirst("preferred_username")?.Value ?? "Unknown",
        Email = claimsPrincipal.FindFirst("email")?.Value ?? claimsPrincipal.FindFirst("preferred_username")?.Value,
        UserInitials = GetUserInitials(claimsPrincipal)
      };

      return await Task.FromResult(user);
    }

    /// <summary>
    /// Gets the Entra Object ID from claims (unique user identifier)
    /// </summary>
    private static string? GetEntraObjectId(System.Security.Claims.ClaimsPrincipal principal)
    {
      return principal.FindFirst("oid")?.Value
          ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
          ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Gets user initials from name claim
    /// </summary>
    private static string GetUserInitials(System.Security.Claims.ClaimsPrincipal principal)
    {
      var name = principal.FindFirst("name")?.Value ?? principal.Identity?.Name ?? "";
      var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      if(parts.Length >= 2)
      {
        return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
      }
      else if(parts.Length == 1 && parts[0].Length > 0)
      {
        return parts[0][0].ToString().ToUpper();
      }

      return "??";
    }
  }
}
