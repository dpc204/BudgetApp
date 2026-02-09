using Azure.Data.Tables;
using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Budget.Api.Features.Utilities.ImportExport;

/// <summary>
/// Gets all backup sets from Azure Table Storage
/// </summary>
public static class GetBackupSets
{
  public sealed record Query : IRequest<Response>;

  public sealed record Response(IReadOnlyList<BackupSetDto> BackupSets);

  public sealed record BackupSetDto(
    string PartitionKey,
    DateTime BackupDate,
    int TableCount,
    long TotalSizeBytes);

  /// <summary>
  /// Handles retrieval of backup sets
  /// </summary>
  public class Handler(
    TableServiceClient tableServiceClient,
    ILogger<Handler> log) : IRequestHandler<Query, Response>
  {
    private const string TableName = "TableBackups";

    public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
    {
      try
      {
        log.LogInformation("Retrieving backup sets from Azure Table Storage");

        var tableClient = tableServiceClient.GetTableClient(TableName);
      
      // Ensure table exists
      try
      {
        await tableClient.CreateIfNotExistsAsync(cancellationToken);
      }
      catch (Exception ex)
      {
        log.LogWarning(ex, "Azure Table Storage not available - backup functionality disabled. Configure AZURE_STORAGE_TABLE_ENDPOINT to enable.");
        return new Response([]);
      }

        // Query all entities and group by PartitionKey
        var backupSets = new Dictionary<string, (DateTime BackupDate, int TableCount, long TotalSize)>();

        await foreach (var entity in tableClient.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
          var partitionKey = entity.PartitionKey;
          var sizeBytes = entity.GetInt32("SizeBytes") ?? 0;
          var exportedAt = entity.GetDateTime("ExportedAt") ?? DateTime.MinValue;

          if (!backupSets.TryGetValue(partitionKey, out (DateTime BackupDate, int TableCount, long TotalSize) current))
          {
            current = (exportedAt, 0, 0);
            backupSets[partitionKey] = current;
          }

          backupSets[partitionKey] = (current.BackupDate, current.TableCount + 1, current.TotalSize + sizeBytes);
        }

        // Convert to DTOs and sort by date descending (newest first)
        var result = backupSets
          .Select(kvp => new BackupSetDto(
            kvp.Key,
            kvp.Value.BackupDate,
            kvp.Value.TableCount,
            kvp.Value.TotalSize))
          .OrderByDescending(x => x.BackupDate)
          .ToList();

        log.LogInformation("Found {Count} backup sets", result.Count);
        return new Response(result);
      }
      catch (Exception e)
      {
        log.LogError("Exception!!:{exception}", e.Message);
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
      app.MapGet("/utilities/backup-sets", async ([FromServices] ISender sender) =>
      {
        try
        {

          var result = await sender.Send(new Query());
          return Results.Ok(result.BackupSets);
        }
        catch (Exception e)
        {
          Console.WriteLine("EXCEPTION:!!!!!!!"+e.Message);
          throw;
        }
      })
      .RequireAuthorization("Admin");
    }
  }
}
