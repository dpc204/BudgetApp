namespace Budget.Api.Features.Admin.Roles;

/// <summary>
/// Deletes a role
/// </summary>
public static class DeleteRole
{
  public sealed record Command(int Id) : IRequest<Response>;

  public sealed record Response(bool Success, string? ErrorMessage = null);

  /// <summary>
  /// Handles deleting a role
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      var role = await db.Roles
        .TagWithCallSite()
        .Include(r => r.UserRoles)
        .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

      if(role == null)
      {
        return new Response(false, "Role not found");
      }

      // Don't allow deletion if users are assigned to this role
      if(role.UserRoles.Count > 0)
      {
        return new Response(false, $"Cannot delete role '{role.Name}' because it has {role.UserRoles.Count} user(s) assigned to it.");
      }

      db.Roles.Remove(role);
      await db.SaveChangesAsync(cancellationToken);

      return new Response(true);
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
        var result = await sender.Send(new Command(id));

        if(!result.Success)
        {
          return result.ErrorMessage == "Role not found"
            ? Results.NotFound(new { error = result.ErrorMessage })
            : Results.BadRequest(new { error = result.ErrorMessage });
        }

        return Results.NoContent();
      })
      .RequireAuthorization("Admin")
      .WithTags("Admin");
    }
  }
}
