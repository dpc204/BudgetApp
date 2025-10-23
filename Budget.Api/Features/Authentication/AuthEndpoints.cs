using Carter;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Authentication;

public sealed class AuthEndpoints : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        // Only register these endpoints if the API Identity stack is present
        var canResolve = app.ServiceProvider.GetService<UserManager<IdentityUser>>() is not null;
        if (!canResolve)
        {
            return; // Host app manages identity; skip JWT auth endpoints
        }

        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("register", async ([FromBody] RegisterRequest req, UserManager<IdentityUser> userManager) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                    return Results.BadRequest("Email and password required");

                var user = new IdentityUser { UserName = req.Email, Email = req.Email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, req.Password);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(result.Errors.Select(e => e.Description));
                }
                return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Email });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.ToString());
            }
        });

        group.MapPost("login", async ([FromBody] LoginRequest req, UserManager<IdentityUser> userManager, IJwtTokenService tokenSvc) =>
        {
            try
            {
                var user = await userManager.FindByEmailAsync(req.Email);
                if (user is null) return Results.Unauthorized();
                if (!await userManager.CheckPasswordAsync(user, req.Password)) return Results.Unauthorized();
                var roles = await userManager.GetRolesAsync(user);
                var token = tokenSvc.CreateToken(user, roles);
                return Results.Ok(token);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.ToString());
            }
        });
    }
}
