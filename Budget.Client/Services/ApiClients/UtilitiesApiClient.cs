namespace Budget.Client.Services.ApiClients;

/// <summary>
/// Implementation of utilities API client
/// </summary>
public sealed class UtilitiesApiClient(HttpClient http, ILogger<UtilitiesApiClient> logger) : IUtilitiesApiClient
{
  // Backup operations
  public async Task<BackupPlanDto> GetBackupPlanAsync(CancellationToken cancellationToken = default)
  {
    var result = await http.GetFromJsonAsync<BackupPlanDto>("/api/maintenance/backup-plan", cancellationToken);
    if(result is null)
    {
      logger.LogDebug("Null response for BackupPlanDto from /api/maintenance/backup-plan");
      throw new InvalidOperationException("Expected non-null BackupPlanDto from '/api/maintenance/backup-plan'.");
    }
    return result;
  }

  public async Task<string> TriggerAzureSqlBackupAsync(CancellationToken cancellationToken = default)
  {
    using var resp = await http.PostAsync("/api/maintenance/backup-azure-sql", null, cancellationToken);
    var body = await resp.Content.ReadAsStringAsync(cancellationToken);
    if(!resp.IsSuccessStatusCode)
    {
      throw new InvalidOperationException($"Backup failed ({(int)resp.StatusCode}): {body}");
    }

    return body;
  }

  public async Task<FileDownloadDto> DownloadDatabaseBackupAsync(string fileName, CancellationToken cancellationToken = default)
  {
    var encodedFileName = Uri.EscapeDataString(fileName);
    using var resp = await http.GetAsync($"/api/maintenance/backup-download?name={encodedFileName}", cancellationToken);
    resp.EnsureSuccessStatusCode();

    var content = await resp.Content.ReadAsByteArrayAsync(cancellationToken);

    return new FileDownloadDto(content, fileName, "application/octet-stream");
  }

  // Import/Export operations
  public async Task<ExportAllResponse> ExportAllTablesAsync(CancellationToken cancellationToken = default)
  {
    var result = await PostAsync<object, ExportAllResponse>("utilities/export-all", new { }, cancellationToken);
    return result;
  }

  public async Task<BackupStatusDto?> GetBackupStatusAsync(string backupId, CancellationToken cancellationToken = default)
  {
    using var resp = await http.GetAsync($"utilities/backup-status/{backupId}", cancellationToken);
    if(resp.StatusCode == System.Net.HttpStatusCode.NotFound)
      return null;

    resp.EnsureSuccessStatusCode();
    var result = await resp.Content.ReadFromJsonAsync<BackupStatusDto>(cancellationToken: cancellationToken);
    return result;
  }

  public async Task<IEnumerable<BackupSetDto>> GetBackupSetsAsync(CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<BackupSetDto>("utilities/backup-sets", cancellationToken);
    return readOnlyList;
  }

  public async Task<IEnumerable<BackupTableDto>> GetBackupSetDetailsAsync(string partitionKey, CancellationToken cancellationToken = default)
  {
    var encodedPartitionKey = Uri.EscapeDataString(partitionKey);
    var readOnlyList = await GetListAsync<BackupTableDto>($"utilities/backup-sets/{encodedPartitionKey}/details", cancellationToken);
    return readOnlyList;
  }

  public async Task<bool> DeleteBackupSetAsync(string partitionKey, CancellationToken cancellationToken = default)
  {
    var encodedPartitionKey = Uri.EscapeDataString(partitionKey);
    using var resp = await http.DeleteAsync($"utilities/backup-sets/{encodedPartitionKey}", cancellationToken);
    if(resp.StatusCode == System.Net.HttpStatusCode.NotFound)
      return false;

    resp.EnsureSuccessStatusCode();
    return true;
  }

  public async Task<FileDownloadDto> DownloadBackupCsvAsync(string blobName, CancellationToken cancellationToken = default)
  {
    var encodedBlobName = Uri.EscapeDataString(blobName);
    using var resp = await http.GetAsync($"utilities/backup-csv/download?blobName={encodedBlobName}", cancellationToken);
    resp.EnsureSuccessStatusCode();

    var content = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
    var fileName = Path.GetFileName(blobName);

    return new FileDownloadDto(content, fileName, "text/csv");
  }

  public async Task<ImportAllResponse> ImportAllAsync(string partitionKey, string targetDatabase, CancellationToken cancellationToken = default)
  {
    var payload = new { PartitionKey = partitionKey, TargetDatabase = targetDatabase };
    var result = await PostAsync<object, ImportAllResponse>("utilities/import-all", payload, cancellationToken);
    return result;
  }

  public async Task<RestoreStatusDto?> GetRestoreStatusAsync(string restoreId, CancellationToken cancellationToken = default)
  {
    using var resp = await http.GetAsync($"utilities/restore-status/{restoreId}", cancellationToken);
    if(resp.StatusCode == System.Net.HttpStatusCode.NotFound)
      return null;

    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<RestoreStatusDto>(cancellationToken: cancellationToken);
  }

  // BACPAC history operations
  public async Task<IEnumerable<BacpacBackupDto>> GetBacpacHistoryAsync(CancellationToken cancellationToken = default)
  {
    return await GetListAsync<BacpacBackupDto>("api/maintenance/bacpac/history", cancellationToken);
  }

  public async Task<string> TriggerBacpacBackupAsync(CancellationToken cancellationToken = default)
  {
    using var resp = await http.PostAsync("/api/maintenance/bacpac/trigger", null, cancellationToken);
    var body = await resp.Content.ReadAsStringAsync(cancellationToken);
    if(!resp.IsSuccessStatusCode)
      throw new InvalidOperationException($"BACPAC backup failed ({(int)resp.StatusCode}): {body}");
    return body;
  }

  public async Task<bool> DeleteBacpacBackupAsync(string rowKey, CancellationToken cancellationToken = default)
  {
    var encodedRowKey = Uri.EscapeDataString(rowKey);
    using var resp = await http.DeleteAsync($"/api/maintenance/bacpac/{encodedRowKey}", cancellationToken);
    if(resp.StatusCode == System.Net.HttpStatusCode.NotFound)
      return false;
    resp.EnsureSuccessStatusCode();
    return true;
  }

  public async Task<FileDownloadDto> DownloadBacpacBackupAsync(string rowKey, CancellationToken cancellationToken = default)
  {
    var encodedRowKey = Uri.EscapeDataString(rowKey);
    using var resp = await http.GetAsync($"/api/maintenance/bacpac/download/{encodedRowKey}", cancellationToken);
    resp.EnsureSuccessStatusCode();

    var content = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
    var fileName = $"{rowKey}.bacpac";

    return new FileDownloadDto(content, fileName, "application/octet-stream");
  }
  public async Task<BudgetSystemInfoDto> GetSystemInfoAsync(CancellationToken cancellationToken = default)
  {
    var result = await http.GetFromJsonAsync<BudgetSystemInfoDto>("/api/system/info", cancellationToken);
    if(result is null)
    {
      logger.LogDebug("Null response for {Type} from {Url}", typeof(BudgetSystemInfoDto).Name, "/api/system/info");
      throw new InvalidOperationException($"Expected non-null {typeof(BudgetSystemInfoDto).Name} from '/api/system/info'.");
    }
    return result;
  }

  // Helper methods
  private async Task<IEnumerable<T>> GetListAsync<T>(string relativeUrl, CancellationToken ct)
  {
    var result = await http.GetFromJsonAsync<List<T>>(relativeUrl, cancellationToken: ct);
    return result ?? [];
  }

  private async Task<TResponse> PostAsync<TRequest, TResponse>(string relativeUrl, TRequest payload, CancellationToken ct)
  {
    using var resp = await http.PostAsJsonAsync(relativeUrl, payload, ct);
    resp.EnsureSuccessStatusCode();
    var result = await resp.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
    if(result is null)
    {
      logger.LogDebug("Null response for {Type} from {Url}", typeof(TResponse).Name, relativeUrl);
      throw new InvalidOperationException($"Expected non-null {typeof(TResponse).Name} from '{relativeUrl}'.");
    }
    return result;
  }
}
