using Budget.Api.Services;
using Carter;
using MediatR;

namespace Budget.Api.Features.SqlBackup;

/// <summary>
/// Trigger Azure SQL Database export to Azure Storage
/// </summary>
public static class TriggerAzureBackup
{
  public sealed record Command : IRequest<IResult>;

  /// <summary>
  /// Handles triggering an Azure SQL backup export
  /// </summary>
  public class Handler(
    BackupAzureSql backup,
    IConfiguration cfg,
    ILogger<Handler> logger) : IRequestHandler<Command, IResult>
  {
    public async Task<IResult> Handle(Command request, CancellationToken cancellationToken)
    {
      try
      {
        var subscriptionId = cfg["AzureSqlSubscriptionId"] ?? string.Empty;
        var resourceGroup = cfg["AzureSqlResourceGroup"] ?? string.Empty;
        var serverName = cfg["AzureSqlServerName"] ?? string.Empty;
        var databaseName = cfg["AzureSqlDatabaseName"] ?? string.Empty;
        var storageKey = cfg["AzureSqlStorageKey"] ?? string.Empty;
        var storageUri = cfg["AzureSqlStorageUri"] ?? string.Empty;
        var dbAdmin = cfg["AzureSqlDbAdmin"] ?? string.Empty;
        var dbPassword = cfg["AzureSqlDbPassword"] ?? string.Empty;

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(subscriptionId)) missing.Add("AzureSqlSubscriptionId");
        if (string.IsNullOrWhiteSpace(resourceGroup)) missing.Add("AzureSqlResourceGroup");
        if (string.IsNullOrWhiteSpace(serverName)) missing.Add("AzureSqlServerName");
        if (string.IsNullOrWhiteSpace(databaseName)) missing.Add("AzureSqlDatabaseName");
        if (string.IsNullOrWhiteSpace(storageKey)) missing.Add("AzureSqlStorageKey");
        if (string.IsNullOrWhiteSpace(storageUri)) missing.Add("AzureSqlStorageUri");
        if (string.IsNullOrWhiteSpace(dbAdmin)) missing.Add("AzureSqlDbAdmin");
        if (string.IsNullOrWhiteSpace(dbPassword)) missing.Add("AzureSqlDbPassword");
        
        if (missing.Count > 0)
        {
          var payload = new { error = "Missing AzureSql configuration values.", missing };
          logger.LogWarning("Backup request rejected due to missing configuration: {Missing}", string.Join(", ", missing));
          return Results.BadRequest(payload);
        }

        // If StorageUri points to a container (or just the account root), append a guaranteed-unique filename
        if (!storageUri.EndsWith(".bacpac", StringComparison.OrdinalIgnoreCase))
        {
          // Normalize base and ensure a container segment
          if (!Uri.TryCreate(storageUri, UriKind.Absolute, out var su))
          {
            return Results.BadRequest(new { error = "AzureSqlStorageUri is not a valid absolute URI.", storageUri });
          }

          var baseUrl = $"{su.Scheme}://{su.Host}";
          var path = su.AbsolutePath?.Trim('/') ?? string.Empty;
          if (string.IsNullOrWhiteSpace(path))
          {
            path = "sqlserver-backups";
            logger.LogInformation("No container specified in StorageUri. Using default container '{Container}'.", path);
          }

          var sep = path.EndsWith('/') ? string.Empty : "/";
          var uniqueName = $"{databaseName}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.bacpac";
          storageUri = $"{baseUrl}/{path}{sep}{uniqueName}";
          logger.LogInformation("Computed export blob path: {Blob}", storageUri);
        }

        var result = await backup.ExportDatabaseAsync(
          subscriptionId,
          resourceGroup,
          serverName,
          databaseName,
          storageKey,
          storageUri,
          dbAdmin,
          dbPassword,
          cancellationToken);
        
        return Results.Ok(result);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Backup failed");
        return Results.Problem(ex.ToString(), statusCode: 500);
      }
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapPost("/api/maintenance/backup-azure-sql", async (ISender sender) =>
      {
        var result = await sender.Send(new Command());
        return result;
      })
      .RequireAuthorization("Admin")
      .WithName("TriggerAzureBackup")
      .WithTags("Maintenance");
    }
  }
}
