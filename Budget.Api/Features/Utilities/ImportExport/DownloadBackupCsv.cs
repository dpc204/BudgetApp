using Azure.Storage.Blobs;
using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Utilities.ImportExport;

/// <summary>
/// Downloads a CSV file from blob storage
/// </summary>
public static class DownloadBackupCsv
{
  public sealed record Query(string BlobName) : IRequest<Response>;

  public sealed record Response(Stream? Content, string? ContentType, string? FileName);

  /// <summary>
  /// Handles CSV download from blob storage
  /// </summary>
  public class Handler(
    BlobServiceClient blobServiceClient,
    ILogger<Handler> log) : IRequestHandler<Query, Response>
  {
    private const string ContainerName = "backups";

    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      log.LogInformation("Downloading CSV from blob: {BlobName}", request.BlobName);

      try
      {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = blobContainerClient.GetBlobClient(request.BlobName);

        // Check if blob exists
        var exists = await blobClient.ExistsAsync(cancellationToken);
        if (!exists)
        {
          log.LogWarning("Blob not found: {BlobName}", request.BlobName);
          return new Response(null, null, null);
        }

        // Download blob content
        var downloadResponse = await blobClient.DownloadAsync(cancellationToken);
        
        // Extract filename from blob name (e.g., "BackupSet-2024-01-06/TableName.csv" -> "TableName.csv")
        var fileName = Path.GetFileName(request.BlobName);

        log.LogInformation("Successfully downloaded blob: {BlobName}", request.BlobName);
        return new Response(downloadResponse.Value.Content, "text/csv", fileName);
      }
      catch (Exception ex)
      {
        log.LogError(ex, "Error downloading blob: {BlobName}", request.BlobName);
        return new Response(null, null, null);
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
      app.MapGet("/utilities/backup-csv/download", async (
        [FromQuery] string blobName,
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query(blobName));
        
        if (result.Content == null)
        {
          return Results.NotFound();
        }

        return Results.File(result.Content, result.ContentType!, result.FileName!);
      });
    }
  }
}
