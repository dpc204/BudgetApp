using Budget.DB;

namespace Budget.Api.Features.Admin.Users;

/// <summary>
/// Gets a single user by ID with their assigned roles
/// </summary>
public static class GetUser
{
  public sealed record Query(int Id) : IRequest<Response?>;

  public sealed record Response(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    int FamilyId,
    List<RoleDto> Roles);

  public sealed record RoleDto(int Id, string Name);

  /// <summary>
  /// Handles retrieving a single user with roles
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response?>
  {
    public async Task<Response?> Handle(Query request, CancellationToken cancellationToken)
    {
      var user = await db.Users
        .IgnoreQueryFilters()
        .Where(u => u.Id == request.Id)
        .Select(u => new Response(
          u.Id,
          u.Email,
          u.FirstName,
          u.LastName,
          u.FamilyId,
          db.UserRoles
            .Where(ur => ur.UserId == u.Id)
            .Select(ur => new RoleDto(ur.Role.Id, ur.Role.Name))
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
      app.MapGet("/api/admin/users/{id:int}", async ([FromRoute] int id, [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query(id));
        return result != null ? Results.Ok(result) : Results.NotFound();
      })
      .RequireAuthorization("Admin")
      .WithTags("Admin");
    }
  }
}
