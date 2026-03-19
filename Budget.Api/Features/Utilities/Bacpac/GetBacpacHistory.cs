using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace Budget.Api.Features.Utilities.Bacpac;

/// <summary>
/// Lists all BACPAC backup records from Azure Table Storage
/// </summary>
public static class GetBacpacHistory
{
  public sealed record Query : IRequest<IEnumerable<BacpacBackupDto>>;

  /// <summary>
  /// Handles listing BACPAC backup records from the BacpacHistory table
  /// </summary>
  public class Handler(
    TableServiceClient tableServiceClient,
    ILogger<Handler> logger) : IRequestHandler<Query, IEnumerable<BacpacBackupDto>>
  {
    private const string TableName = "BacpacHistory";

    public async Task<IEnumerable<BacpacBackupDto>> Handle(Query request, CancellationToken cancellationToken)
    {
      try
      {
        var tableClient = tableServiceClient.GetTableClient(TableName);
        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        var results = new List<BacpacBackupDto>();
        await foreach (var entity in tableClient.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
          var dto = new BacpacBackupDto(
            RowKey: entity.RowKey,
            DatabaseName: entity.GetString("DatabaseName") ?? entity.PartitionKey,
            CreatedAt: entity.GetDateTimeOffset("CreatedAt")?.UtcDateTime ?? entity.Timestamp?.UtcDateTime ?? DateTime.UtcNow,
            SizeBytes: entity.GetInt64("SizeBytes") ?? 0L,
            BlobName: entity.GetString("BlobName") ?? string.Empty);
          results.Add(dto);
        }

        return results.OrderByDescending(x => x.CreatedAt);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error listing BACPAC history");
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
      app.MapGet("/api/maintenance/bacpac/history", async (ISender sender) =>
      {
        var result = await sender.Send(new Query());
        return Results.Ok(result);
      })
      .WithName("GetBacpacHistory")
      .WithTags("Maintenance")
      .RequireAuthorization("Admin");
    }
  }
}
