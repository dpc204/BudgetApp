using System.Security.Claims;

namespace Budget.Api.Features.Admin.UserRoles;

/// <summary>
/// Assigns a role to a user
/// </summary>
public static class AssignRole
{
  public sealed record Command(int UserId, int RoleId) : IRequest<Response>;

  public sealed record Response(int UserId, int RoleId, string RoleName, DateTime AssignedAt);

  /// <summary>
  /// Handles assigning a role to a user
  /// </summary>
  public class Handler(BudgetContext db, IHttpContextAccessor httpContextAccessor) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      // Check if user exists
      var userExists = await db.Users
        .IgnoreQueryFilters()
        .AnyAsync(u => u.Id == request.UserId, cancellationToken);
      
      if (!userExists)
      {
        throw new InvalidOperationException($"User with ID {request.UserId} not found");
      }

      // Check if role exists
      var role = await db.Roles.FindAsync([request.RoleId], cancellationToken) ?? throw new InvalidOperationException($"Role with ID {request.RoleId} not found");

      // Check if assignment already exists
      var existingAssignment = await db.UserRoles
        .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);

      if (existingAssignment != null)
      {
        throw new InvalidOperationException($"User already has the '{role.Name}' role assigned");
      }

      // Get current user ID for audit trail
      var currentUserEmail = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
      int? assignedByUserId = null;
      
      if (!string.IsNullOrEmpty(currentUserEmail))
      {
        currentUserEmail = currentUserEmail.ToUpper();

        var currentUser = await db.Users
          .IgnoreQueryFilters()
          .FirstOrDefaultAsync(u => u.Email.Equals(currentUserEmail), cancellationToken);
        assignedByUserId = currentUser?.Id;
      }

      // Create assignment
      var userRole = new UserRole
      {
        UserId = request.UserId,
        RoleId = request.RoleId,
        AssignedAt = DateTime.UtcNow,
        AssignedByUserId = assignedByUserId
      };

      db.UserRoles.Add(userRole);
      await db.SaveChangesAsync(cancellationToken);

      return new Response(request.UserId, request.RoleId, role.Name, userRole.AssignedAt);
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/api/admin/users/{userId:int}/roles/{roleId:int}", async (
        [FromRoute] int userId,
        [FromRoute] int roleId,
        [FromServices] ISender sender) =>
      {
        try
        {
          var result = await sender.Send(new Command(userId, roleId));
          return Results.Ok(result);
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
