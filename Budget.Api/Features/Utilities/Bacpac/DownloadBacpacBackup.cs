using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace Budget.Api.Features.Utilities.Bacpac;

/// <summary>
/// Downloads a BACPAC backup file from Azure Blob Storage
/// </summary>
public static class DownloadBacpacBackup
{
  public sealed record Query(string RowKey) : IRequest<IResult>;

  /// <summary>
  /// Handles downloading a BACPAC backup from blob storage
  /// </summary>
  public class Handler(
    BudgetContext db,
    BlobServiceClient blobServiceClient,
    TableServiceClient tableServiceClient,
    ILogger<Handler> logger) : IRequestHandler<Query, IResult>
  {
    private const string ContainerName = "bacpac-backups";
    private const string TableName = "BacpacHistory";

    public async Task<IResult> Handle(Query request, CancellationToken cancellationToken)
    {
      var databaseName = db.Database.GetDbConnection().Database;

      try
      {
        var tableClient = tableServiceClient.GetTableClient(TableName);

        // Get the entity to find the blob name
        TableEntity? entity;
        try
        {
          var response = await tableClient.GetEntityAsync<TableEntity>(
            databaseName, request.RowKey, cancellationToken: cancellationToken);
          entity = response.Value;
        }
        catch(Azure.RequestFailedException ex) when(ex.Status == 404)
        {
          logger.LogWarning("BACPAC history entry not found: {RowKey}", request.RowKey);
          return Results.NotFound();
        }

        var blobName = entity.GetString("BlobName") ?? string.Empty;
        if(string.IsNullOrWhiteSpace(blobName))
        {
          logger.LogError("BACPAC entry {RowKey} has no BlobName", request.RowKey);
          return Results.Problem("Blob name not found for this backup entry.", statusCode: 500);
        }

        // Download the blob
        var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if(!await blobClient.ExistsAsync(cancellationToken))
        {
          logger.LogWarning("BACPAC blob not found: {BlobName}", blobName);
          return Results.NotFound($"Backup file '{blobName}' not found in storage.");
        }

        var download = await blobClient.DownloadContentAsync(cancellationToken);
        var content = download.Value.Content.ToArray();
        var fileName = Path.GetFileName(blobName);

        logger.LogInformation("Streaming BACPAC download: {BlobName} ({Size} bytes)", blobName, content.Length);
        return Results.File(content, "application/octet-stream", fileName);
      }
      catch(Exception ex)
      {
        logger.LogError(ex, "Error downloading BACPAC backup {RowKey}", request.RowKey);
        return Results.Problem(ex.Message, statusCode: 500);
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
      app.MapGet("/api/maintenance/bacpac/download/{rowKey}", async (ISender sender, string rowKey) =>
      {
        var result = await sender.Send(new Query(rowKey));
        return result;
      })
      .WithName("DownloadBacpacBackup")
      .WithTags("Maintenance")
      .RequireAuthorization("Admin");
    }
  }
}
