namespace Budget.Api.Features.Admin.Roles;

/// <summary>
/// Updates an existing role
/// </summary>
public static class UpdateRole
{
  public sealed record Command(int Id, string Name, string Description) : IRequest<Response?>;

  public sealed record Response(int Id, string Name, string Description);

  /// <summary>
  /// Handles updating a role
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response?>
  {
    public async Task<Response?> Handle(Command request, CancellationToken cancellationToken)
    {
      var role = await db.Roles.FindAsync([request.Id], cancellationToken);
      if (role == null)
      {
        return null;
      }

      role.Name = request.Name;
      role.Description = request.Description;
      role.ModifiedAt = DateTime.UtcNow;

      await db.SaveChangesAsync(cancellationToken);

      return new Response(role.Id, role.Name, role.Description);
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/api/admin/roles/{id:int}", async (
        [FromRoute] int id,
        [FromBody] Command command,
        [FromServices] ISender sender) =>
      {
        if (id != command.Id)
        {
          return Results.BadRequest("ID mismatch");
        }

        var result = await sender.Send(command);
        return result != null ? Results.Ok(result) : Results.NotFound();
      })
      .RequireAuthorization("Admin")
      .WithTags("Admin");
    }
  }
}
