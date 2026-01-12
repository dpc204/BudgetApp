using Budget.DB;

namespace Budget.Api.Features.Admin.Roles;

/// <summary>
/// Gets a single role by ID
/// </summary>
public static class GetRole
{
  public sealed record Query(int Id) : IRequest<Response?>;

  public sealed record Response(
    int Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

  /// <summary>
  /// Handles retrieving a single role
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response?>
  {
    public async Task<Response?> Handle(Query request, CancellationToken cancellationToken)
    {
      var role = await db.Roles
        .Where(r => r.Id == request.Id)
        .Select(r => new Response(
          r.Id,
          r.Name,
          r.Description,
          r.CreatedAt,
          r.ModifiedAt
        ))
        .FirstOrDefaultAsync(cancellationToken);

      return role;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/api/admin/roles/{id:int}", async ([FromRoute] int id, [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query(id));
        return result != null ? Results.Ok(result) : Results.NotFound();
      })
      .RequireAuthorization("Admin")
      .WithTags("Admin");
    }
  }
}
