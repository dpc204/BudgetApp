using Budget.DB;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Budget.Api.Features.UserOptions;

/// <summary>
/// Gets user options from the database
/// </summary>
public static class GetUserOptions
{
  /// <summary>
  /// Query to get user options
  /// </summary>
  public sealed record Query(string UserId) : IRequest<Response>;

  /// <summary>
  /// Response containing user options
  /// </summary>
  public sealed record Response(Budget.Shared.Services.UserOptions? Options);

  /// <summary>
  /// Handles retrieving user options from the database
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      var savedOptions = await db.SavedUserOptions
        .AsNoTracking()
        .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

      if (savedOptions == null)
      {
        return new Response(null);
      }

      var options = JsonSerializer.Deserialize<Budget.Shared.Services.UserOptions>(savedOptions.JsonOptions);
      return new Response(options);
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/api/useroptions/{userId}", async (string userId, [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query(userId));
        return Results.Ok(result);
      })
      .WithTags("UserOptions");
    }
  }
}
