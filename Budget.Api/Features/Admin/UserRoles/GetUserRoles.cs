using Mapster;

namespace Budget.Api.Features.Admin.UserRoles;

/// <summary>
/// Gets all role assignments for a specific user
/// </summary>
public static class GetUserRoles
{
  public sealed record Query(int UserId) : IRequest<Response>;

  public sealed record Response(int UserId, List<RoleDto> Roles);

  public sealed record RoleDto(
    int RoleId,
    string RoleName,
    DateTime AssignedAt,
    int? AssignedByUserId,
    string? AssignedByName);

  /// <summary>
  /// Handles retrieving user role assignments
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      //var roles = await db.UserRoles
      //  .Where(ur => ur.UserId == request.UserId)
      //  .Select(ur => new RoleDto(
      //    ur.RoleId,
      //    ur.Role.Name,
      //    ur.AssignedAt,
      //    ur.AssignedByUserId,
      //    ur.AssignedBy != null ? $"{ur.AssignedBy.FirstName} {ur.AssignedBy.LastName}" : null
      //  ))
      //  .OrderBy(r => r.RoleName)
      //  .ToListAsync(cancellationToken);

      var roles = await db.UserRoles.Include(a => a.AssignedBy)
        .Where(ur => ur.UserId == request.UserId).Select(u => u).ToListAsync(cancellationToken);


      var config = new TypeAdapterConfig();

      config.NewConfig<UserRole, RoleDto>()
        .Map(dest => dest.AssignedByName,
          src => GetName(src.AssignedBy));


      var rolesDto = roles.Adapt<List<RoleDto>>(config);

      return new Response(request.UserId, rolesDto);
    }

    private string GetName(User? srcAssignedBy)
    {

      if (srcAssignedBy == null)
        return "AssignedBy is null";
      
      return srcAssignedBy.FirstName + " " + srcAssignedBy.LastName;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/api/admin/users/{userId:int}/roles", async (
          [FromRoute] int userId,
          [FromServices] ISender sender) =>
        {
          var result = await sender.Send(new Query(userId));
          return Results.Ok(result);
        })
        .RequireAuthorization("Admin")
        .WithTags("Admin");
    }
  }
}