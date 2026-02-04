using Budget.Shared.Services;

namespace Budget.Api.Features.UserOptions;

/// <summary>
/// Gets a single user by email with their assigned roles
/// </summary>
public static class GetUserByEmail
{
  public sealed record Query(string UserEmail) : IRequest<UserDetailDto?>;

  /// <summary>
  /// Handles retrieving a single user with roles
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, UserDetailDto?>
  {
    public async Task<UserDetailDto?> Handle(Query request, CancellationToken cancellationToken)
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

      return user;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("api/useroptions/GetUserByEmail",
          async ([FromQuery] string userEmail, [FromServices] ISender sender) =>
          {
            var result = await sender.Send(new Query(userEmail));
            return result != null ? Results.Ok(result) : Results.NotFound();
          })
        .RequireAuthorization();
    }
  }
}