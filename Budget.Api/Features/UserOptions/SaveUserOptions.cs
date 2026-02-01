using Carter;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Budget.Api.Features.UserOptions;

/// <summary>
/// Saves user options to the database
/// </summary>
public static class SaveUserOptions
{
  /// <summary>
  /// Command to save user options
  /// </summary>
  public sealed record Command(int UserId, Budget.Shared.Services.UserOptions Options) : IRequest<Response>;

  /// <summary>
  /// Response indicating save success
  /// </summary>
  public sealed record Response(bool Success);

  /// <summary>
  /// Handles saving user options to the database
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Command, Response>
  {
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      var jsonOptions = JsonSerializer.Serialize(request.Options);

      var existingOptions = await db.SavedUserOptions.FindAsync([request.UserId], cancellationToken);

      if (existingOptions != null)
      {
        existingOptions.JsonOptions = jsonOptions;
      }
      else
      {
        db.SavedUserOptions.Add(new SavedUserOptions
        {
          UserId = request.UserId,
          JsonOptions = jsonOptions
        });
      }

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
      app.MapPost("/api/useroptions", async ([FromServices] ISender sender, [FromBody] Command command) =>
      {
        var result = await sender.Send(command);
        return Results.Ok(result);
      })
      .WithTags("UserOptions")
      .RequireAuthorization();
    }
  }
}
