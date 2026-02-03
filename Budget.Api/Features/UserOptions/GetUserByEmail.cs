namespace Budget.Api.Features.UserOptions;

/// <summary>
/// Gets a single user by ID with their assigned roles
/// </summary>
public static class GetUserByEmail
{
  public sealed record Query(string UserEmail) : IRequest<Response?>;

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
        .Where(u => u.Email == request.UserEmail)
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
      app.MapPost("api/useroptions/GetUserByEmail",
          async ([FromQuery] string userEmail, [FromServices] ISender sender) =>
          {
            var result = await sender.Send(new Query(userEmail));
            return result != null ? Results.Ok(result) : Results.NotFound();
          })
        .RequireAuthorization();
    }
  }
}