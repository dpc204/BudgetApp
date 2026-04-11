using Budget.Shared.Services;

namespace Budget.Api.Features.Utilities.SystemInfo;

/// <summary>
/// Gets system information about the Budget application environment
/// </summary>
public static class GetSystemInfo
{
  public sealed record Query : IRequest<Response>;

  public sealed record Response(BudgetSystemInfoDto SystemInfo);

  /// <summary>
  /// Handles retrieving system information including database environment
  /// </summary>
  public class Handler(IConfiguration configuration, IHostEnvironment hostEnvironment, ILogger<Handler> logger) : IRequestHandler<Query, Response>
  {
    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      var useAzureDb = DetermineIfUsingAzureDatabase();
      var databaseEnvironment = useAzureDb ? "Azure" : "Local";

      var systemInfo = new BudgetSystemInfoDto(
        UseAzureDB: useAzureDb,
        DatabaseEnvironment: databaseEnvironment,
        IsDevelopment: hostEnvironment.IsDevelopment());

      return await Task.FromResult(new Response(systemInfo));
    }

    private bool DetermineIfUsingAzureDatabase()
    {
      // Check if running on Azure
      if(AzureEnvironment.IsRunningOnAzure)
      {
        logger.LogDebug("Running on Azure environment");
        return true;
      }

      // Check configuration setting
      var useAzureDbConfig = configuration["UseAzureDB"]?.ToLower();
      logger.LogDebug("UseAzureDB configuration value: {UseAzureDB}", useAzureDbConfig);

      var isAzure = bool.TryParse(useAzureDbConfig, out var value) && value;
      return isAzure;
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/api/system/info", async (ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result.SystemInfo);
      })
      .WithName("GetSystemInfo")
      .WithTags("System")
      .RequireAuthorization("Admin");
    }
  }
}
