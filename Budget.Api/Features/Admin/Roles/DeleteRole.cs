using Budget.DB;

namespace Budget.Api.Features.Admin.Roles;

/// <summary>
/// Deletes a role
/// </summary>
public static class DeleteRole
{
  public sealed record Command(int Id) : IRequest<bool>;

  /// <summary>
  /// Handles deleting a role
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, bool>
  {
    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
      var role = await db.Roles
        .Include(r => r.UserRoles)
        .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

      if (role == null)
      {
        return false;
      }

      // Don't allow deletion if users are assigned to this role
      if (role.UserRoles.Count > 0)
      {
        throw new InvalidOperationException($"Cannot delete role '{role.Name}' because it has {role.UserRoles.Count} user(s) assigned to it.");
      }

      db.Roles.Remove(role);
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
      app.MapDelete("/api/admin/roles/{id:int}", async ([FromRoute] int id, [FromServices] ISender sender) =>
      {
        try
        {
          var result = await sender.Send(new Command(id));
          return result ? Results.NoContent() : Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new { error = ex.Message });
        }
      })
      .RequireAuthorization("Admin")
      .WithTags("Admin");
    }
  }
}
