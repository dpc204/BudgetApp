using Budget.DB;

namespace Budget.Api.Features.Admin.Roles;

/// <summary>
/// Creates a new role
/// </summary>
public static class CreateRole
{
  public sealed record Command(string Name, string Description) : IRequest<Response>;

  public sealed record Response(int Id, string Name, string Description);

  /// <summary>
  /// Handles creating a new role
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      var role = new Role
      {
        Name = request.Name,
        Description = request.Description,
        CreatedAt = DateTime.UtcNow
      };

      db.Roles.Add(role);
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
      app.MapPost("/api/admin/roles", async ([FromBody] Command command, [FromServices] ISender sender) =>
      {
        var result = await sender.Send(command);
        return Results.Created($"/api/admin/roles/{result.Id}", result);
      })
      .RequireAuthorization("Admin")
      .WithTags("Admin");
    }
  }
}
