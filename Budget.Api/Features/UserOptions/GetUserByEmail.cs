using Budget.Shared.Services;
using Carter;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.UserOptions;

/// <summary>
/// Gets a single user by email with their assigned roles
/// </summary>
public static class GetUserByEmail
{
  public sealed record Query(string UserEmail) : IRequest<Response>;

  public sealed record Response(UserDetailDto? User);

  /// <summary>
  /// Handles retrieving a single user with roles
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      // Normalize email for case-insensitive comparison
      var normalizedEmail = request.UserEmail.ToUpperInvariant();

      var user = await db.Users
        .IgnoreQueryFilters()
        .Where(u => u.Email.ToUpper() == normalizedEmail)
        .Select(u => new UserDetailDto(
          u.Id,
          u.Email,
          u.FirstName,
          u.LastName,
          u.FamilyId,
          db.UserRoles
            .Where(ur => ur.UserId == u.Id)
            .Select(ur => new RoleInfoDto(ur.Role.Id, ur.Role.Name))
            .ToList()
        ))
        .FirstOrDefaultAsync(cancellationToken);

      return new Response(user);
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {

      app.MapGet("/api/useroptions/GetUserByEmail",
          async ([FromQuery] string userEmail, [FromServices] ISender sender) =>
          {
            var result = await sender.Send(new Query(userEmail));
            return Results.Ok(result);
          })
        .WithTags("UserOptions")
        .RequireAuthorization();
    }
  }
}