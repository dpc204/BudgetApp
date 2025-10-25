using Carter;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Budget.Shared;

namespace Budget.Api.Features.Authentication;

public sealed class UserInfoDto
{
  public string? Id { get; set; }
  public string? Email { get; set; }
  public string? Name { get; set; }
  public IList<string> Roles { get; set; } = new List<string>();
}

public sealed class UserInfoEndpoints : CarterModule
{
  public override void AddRoutes(IEndpointRouteBuilder app)
  {
    // Avoid resolving scoped services from the root provider
    using var scope = app.ServiceProvider.CreateScope();
    var canResolve = scope.ServiceProvider.GetService<UserManager<BudgetUser>>() is not null;
    if (!canResolve)
      return;

    var group = app.MapGroup("/api/auth").WithTags("Auth");

    group.MapGet("userinfo", async (ClaimsPrincipal user, UserManager<BudgetUser> userManager) =>
    {
      if (user?.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

      var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

      var identityUser = await userManager.FindByIdAsync(userId);
      if (identityUser is null)
        return Results.Unauthorized();

      var roles = await userManager.GetRolesAsync(identityUser);
      var dto = new UserInfoDto
      {
        Id = identityUser.Id,
        Email = identityUser.Email,
        Name = identityUser.UserName,
        Roles = roles
      };
      return Results.Ok(dto);
    });
  }
}