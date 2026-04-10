using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace Budget.Api.Features.Utilities.Bacpac;

/// <summary>
/// Deletes a BACPAC backup: removes the blob from Azure Storage and the record from Azure Table Storage
/// </summary>
public static class DeleteBacpacBackup
{
  public sealed record Command(string RowKey) : IRequest<bool>;

  /// <summary>
  /// Handles deleting a BACPAC backup record and its associated blob
  /// </summary>
  public class Handler(
    BudgetContext db,
    BlobServiceClient blobServiceClient,
    TableServiceClient tableServiceClient,
    ILogger<Handler> logger) : IRequestHandler<Command, bool>
  {
    private const string ContainerName = "bacpac-backups";
    private const string TableName = "BacpacHistory";

    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
      var databaseName = db.Database.GetDbConnection().Database;

      try
      {
        var tableClient = tableServiceClient.GetTableClient(TableName);

        // Get the entity to find the blob name
        TableEntity? entity = null;
        try
        {
          var response = await tableClient.GetEntityAsync<TableEntity>(
            databaseName, request.RowKey, cancellationToken: cancellationToken);
          entity = response.Value;
        }
        catch(Azure.RequestFailedException ex) when(ex.Status == 404)
        {
          logger.LogWarning("BACPAC history entry not found: {RowKey}", request.RowKey);
          return false;
        }

        // Delete the blob
        var blobName = entity.GetString("BlobName") ?? string.Empty;
        if(!string.IsNullOrWhiteSpace(blobName))
        {
          var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
          var blobClient = containerClient.GetBlobClient(blobName);
          await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
          logger.LogInformation("Deleted BACPAC blob: {BlobName}", blobName);
        }

        // Delete the table entry
        await tableClient.DeleteEntityAsync(databaseName, request.RowKey, cancellationToken: cancellationToken);
        logger.LogInformation("Deleted BACPAC table entry: {RowKey}", request.RowKey);

        return true;
      }
      catch(Exception ex)
      {
        logger.LogError(ex, "Error deleting BACPAC backup {RowKey}", request.RowKey);
        throw;
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
      app.MapDelete("/api/maintenance/bacpac/{rowKey}", async (ISender sender, string rowKey) =>
      {
        var result = await sender.Send(new Command(rowKey));
        return result ? Results.Ok() : Results.NotFound();
      })
      .WithName("DeleteBacpacBackup")
      .WithTags("Maintenance")
      .RequireAuthorization("Admin");
    }
  }
}
