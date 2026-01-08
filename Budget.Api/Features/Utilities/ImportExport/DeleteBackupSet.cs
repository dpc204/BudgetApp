using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Utilities.ImportExport;

/// <summary>
/// Deletes a backup set (all table records and blobs)
/// </summary>
public static class DeleteBackupSet
{
  public sealed record Command(string PartitionKey) : IRequest<Response>;

  public sealed record Response(bool Success, string Message);

  /// <summary>
  /// Handles deletion of a backup set
  /// </summary>
  public class Handler(
    BlobServiceClient blobServiceClient,
    TableServiceClient tableServiceClient,
    ILogger<Handler> log) : IRequestHandler<Command, Response>
  {
    private const string ContainerName = "backups";
    private const string TableName = "TableBackups";

    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
      log.LogInformation("Deleting backup set: {PartitionKey}", request.PartitionKey);

      try
      {
        var tableClient = tableServiceClient.GetTableClient(TableName);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

        // Ensure table exists
        try
        {
          await tableClient.CreateIfNotExistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
          log.LogError(ex, "Failed to access TableBackups table");
          return new Response(false, "Failed to access backup table");
        }

        // Query all entities for this PartitionKey
        var filter = $"PartitionKey eq '{request.PartitionKey}'";
        var entitiesToDelete = new List<TableEntity>();

        await foreach (var entity in tableClient.QueryAsync<TableEntity>(filter, cancellationToken: cancellationToken))
        {
          entitiesToDelete.Add(entity);
        }

        if (entitiesToDelete.Count == 0)
        {
          log.LogWarning("No entities found for PartitionKey: {PartitionKey}", request.PartitionKey);
          return new Response(false, "Backup set not found");
        }

        log.LogInformation("Found {Count} entities to delete", entitiesToDelete.Count);

        // Delete blobs first
        var blobsDeleted = 0;
        var blobsFailed = 0;

        foreach (var entity in entitiesToDelete)
        {
          var blobName = entity.GetString("BlobName");
          if (!string.IsNullOrEmpty(blobName))
          {
            try
            {
              var blobClient = blobContainerClient.GetBlobClient(blobName);
              await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
              blobsDeleted++;
              log.LogInformation("Deleted blob: {BlobName}", blobName);
            }
            catch (Exception ex)
            {
              log.LogError(ex, "Failed to delete blob: {BlobName}", blobName);
              blobsFailed++;
            }
          }
        }

        // Delete table entities
        var entitiesDeleted = 0;
        var entitiesFailed = 0;

        foreach (var entity in entitiesToDelete)
        {
          try
          {
            await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: cancellationToken);
            entitiesDeleted++;
          }
          catch (Exception ex)
          {
            log.LogError(ex, "Failed to delete entity: {PartitionKey}/{RowKey}", entity.PartitionKey, entity.RowKey);
            entitiesFailed++;
          }
        }

        log.LogInformation("Deletion complete. Blobs: {BlobsDeleted}/{TotalBlobs}, Entities: {EntitiesDeleted}/{TotalEntities}",
          blobsDeleted, blobsDeleted + blobsFailed, entitiesDeleted, entitiesDeleted + entitiesFailed);

        if (entitiesFailed > 0 || blobsFailed > 0)
        {
          return new Response(false, $"Partial deletion: {entitiesDeleted} entities and {blobsDeleted} blobs deleted, {entitiesFailed + blobsFailed} failures");
        }

        return new Response(true, $"Successfully deleted backup set with {entitiesDeleted} tables");
      }
      catch (Exception ex)
      {
        log.LogError(ex, "Error deleting backup set: {PartitionKey}", request.PartitionKey);
        return new Response(false, $"Error: {ex.Message}");
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
      app.MapDelete("/utilities/backup-sets/{partitionKey}", async (
        [FromRoute] string partitionKey,
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Command(partitionKey));
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
      });
    }
  }
}
