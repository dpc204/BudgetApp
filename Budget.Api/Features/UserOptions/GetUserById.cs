using Budget.Shared.Services;

namespace Budget.Api.Features.UserOptions;

/// <summary>
/// Gets a single user by email with their assigned roles
/// </summary>
public static class GetUserById
{
  public sealed record Query(int Id) : IRequest<Response>;

  public sealed record Response(UserDetailDto? User);

  /// <summary>
  /// Handles retrieving a single user with roles
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {

      var user = await db.Users
        .IgnoreQueryFilters()
        .Where(u => u.Id == request.Id)
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

      app.MapGet("/api/useroptions/GetUserById",
          async ([FromQuery] int id, [FromServices] ISender sender) =>
          {
            var result = await sender.Send(new Query(id));
            return Results.Ok(result);
          })
        .WithTags("UserOptions")
        .RequireAuthorization();
    }
  }
}