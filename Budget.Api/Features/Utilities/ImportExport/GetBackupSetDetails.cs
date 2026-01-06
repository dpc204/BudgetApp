using Azure.Data.Tables;
using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Utilities.ImportExport;

/// <summary>
/// Gets details of tables in a specific backup set
/// </summary>
public static class GetBackupSetDetails
{
  public sealed record Query(string PartitionKey) : IRequest<Response>;

  public sealed record Response(IReadOnlyList<BackupTableDto> Tables);

  public sealed record BackupTableDto(
    string TableName,
    string BlobName,
    long SizeBytes,
    DateTime ExportedAt,
    string PartitionKey);

  /// <summary>
  /// Handles retrieval of backup set details
  /// </summary>
  public class Handler(
    TableServiceClient tableServiceClient,
    ILogger<Handler> log) : IRequestHandler<Query, Response>
  {
    private const string TableName = "TableBackups";

    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      log.LogInformation("Retrieving backup set details for PartitionKey: {PartitionKey}", request.PartitionKey);

      var tableClient = tableServiceClient.GetTableClient(TableName);
      
      // Ensure table exists
      try
      {
        await tableClient.CreateIfNotExistsAsync(cancellationToken);
      }
      catch (Exception ex)
      {
        log.LogError(ex, "Failed to access TableBackups table");
        return new Response([]);
      }

      // Query entities by PartitionKey
      var filter = $"PartitionKey eq '{request.PartitionKey}'";
      var tables = new List<BackupTableDto>();

      await foreach (var entity in tableClient.QueryAsync<TableEntity>(filter, cancellationToken: cancellationToken))
      {
        var tableName = entity.RowKey;
        var blobName = entity.GetString("BlobName") ?? string.Empty;
        var sizeBytes = entity.GetInt32("SizeBytes") ?? 0;
        var exportedAt = entity.GetDateTime("ExportedAt") ?? DateTime.MinValue;

        tables.Add(new BackupTableDto(tableName, blobName, sizeBytes, exportedAt, request.PartitionKey));
      }

      // Sort by table name
      var result = tables.OrderBy(x => x.TableName).ToList();

      log.LogInformation("Found {Count} tables in backup set {PartitionKey}", result.Count, request.PartitionKey);
      return new Response(result);
    }
  }

  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/utilities/backup-sets/{partitionKey}/details", async (
        [FromRoute] string partitionKey,
        [FromServices] ISender sender) =>
      {
        var result = await sender.Send(new Query(partitionKey));
        return Results.Ok(result.Tables);
      })
      .RequireAuthorization("AdminOnly");
    }
  }
}
