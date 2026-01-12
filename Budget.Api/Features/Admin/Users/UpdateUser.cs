using Budget.DB;

namespace Budget.Api.Features.Admin.Users;

/// <summary>
/// Updates user information
/// </summary>
public static class UpdateUser
{
  public sealed record Command(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    int FamilyId) : IRequest<Response?>;

  public sealed record Response(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    int FamilyId);

  /// <summary>
  /// Handles updating user information
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response?>
  {
    public async Task<Response?> Handle(Command request, CancellationToken cancellationToken)
    {
      var user = await db.Users
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

      if (user == null)
      {
        return null;
      }

      user.Email = request.Email;
      user.FirstName = request.FirstName;
      user.LastName = request.LastName;
      user.FamilyId = request.FamilyId;

      await db.SaveChangesAsync(cancellationToken);

      return new Response(user.Id, user.Email, user.FirstName, user.LastName, user.FamilyId);
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPut("/api/admin/users/{id:int}", async (
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
