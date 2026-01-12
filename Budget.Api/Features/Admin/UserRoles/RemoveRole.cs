using Budget.DB;

namespace Budget.Api.Features.Admin.UserRoles;

/// <summary>
/// Removes a role from a user
/// </summary>
public static class RemoveRole
{
  public sealed record Command(int UserId, int RoleId) : IRequest<bool>;

  /// <summary>
  /// Handles removing a role from a user
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, bool>
  {
    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
      var userRole = await db.UserRoles
        .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);

      if (userRole == null)
      {
        return false;
      }

      db.UserRoles.Remove(userRole);
      await db.SaveChangesAsync(cancellationToken);

      return true;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapDelete("/api/admin/users/{userId:int}/roles/{roleId:int}", async (
        [FromRoute] int userId,
        [FromRoute] int roleId,
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Command(userId, roleId));
        return result ? Results.NoContent() : Results.NotFound();
      })
      .RequireAuthorization("Admin")
      .WithTags("Admin");
    }
  }
}
