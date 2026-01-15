using Mapster;

namespace Budget.Api.Features.Admin.Roles;

/// <summary>
/// Gets all roles in the system
/// </summary>
public static class GetRoles
{
  public sealed record Query : IRequest<Response>;

  public sealed record Response(List<RoleDto> Roles);

  public sealed record RoleDto(
    int Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    int UserCount);

  /// <summary>
  /// Handles retrieving all roles with user counts
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      var roles = await db.Roles.Include(a=> a.UserRoles)
        .ToListAsync(cancellationToken);
      
      var rolesDTO = roles.Adapt<List<RoleDto>>()
        .Select(r => r with { UserCount = roles.First(a => a.Id == r.Id).UserRoles.Count })
        .ToList();

      return new Response(rolesDTO);
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/api/admin/roles", async ([FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      })
      .RequireAuthorization("Admin")
      .WithTags("Admin");
    }
  }
}
