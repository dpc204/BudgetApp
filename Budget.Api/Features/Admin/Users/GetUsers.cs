namespace Budget.Api.Features.Admin.Users;

/// <summary>
/// Gets all users with their assigned roles
/// </summary>
public static class GetUsers
{
  public sealed record Query : IRequest<Response>;

  public sealed record Response(List<UserDto> Users);

  public sealed record UserDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    int FamilyId,
    List<string> Roles);

  /// <summary>
  /// Handles retrieving all users with their roles
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      // Use EF Core Select projection instead of Mapster to avoid global state issues
      var userDtos = await db.Users
        .Include(u => u.UserRoles)
          .ThenInclude(ur => ur.Role)
        .OrderBy(u => u.Email)
        .Select(u => new UserDto(
          u.Id,
          u.Email,
          u.FirstName,
          u.LastName,
          u.FamilyId,
          u.UserRoles.Select(ur => ur.Role.Name).ToList()
        ))
        .ToListAsync(cancellationToken);

      return new Response(userDtos);
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/api/admin/users", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      })
      .RequireAuthorization("Admin")
      .WithTags("Admin");
    }
  }
}
