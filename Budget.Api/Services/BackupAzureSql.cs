namespace Budget.Api.Services;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Core;
using Azure.Identity;

/// <summary>
/// Service to export an Azure SQL Database to a .bacpac in Azure Storage using Azure Resource Manager REST API.
/// </summary>
public class BackupAzureSql
{
  private readonly HttpClient _httpClient;
  private readonly IConfiguration _configuration;
  private readonly ILogger<BackupAzureSql> _logger;

  public BackupAzureSql(HttpClient httpClient, IConfiguration configuration, ILogger<BackupAzureSql> logger)
  {
    _httpClient = httpClient;
    _configuration = configuration;
    _logger = logger;
  }

  /// <summary>
  /// Triggers an export of the specified Azure SQL database to the given Storage Blob URI.
  /// </summary>
  /// <param name="subscriptionId">Azure subscription Id.</param>
  /// <param name="resourceGroup">Resource group name containing the SQL server.</param>
  /// <param name="serverName">Azure SQL logical server name.</param>
  /// <param name="databaseName">Database name to export.</param>
  /// <param name="storageKey">Storage account access key.</param>
  /// <param name="storageUri">Destination Blob URI for the .bacpac file (e.g., https://account.blob.core.windows.net/container/file.bacpac).</param>
  /// <param name="dbAdmin">SQL admin login.</param>
  /// <param name="dbPassword">SQL admin password.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A status URL to poll, or the raw response content if no Location header is provided.</returns>
  public async Task<string> ExportDatabaseAsync(
    string subscriptionId,
    string resourceGroup,
    string serverName,
    string databaseName,
    string storageKey,
    string storageUri,
    string dbAdmin,
    string dbPassword,
    CancellationToken cancellationToken = default)
  {
    // Acquire Azure AD token for Azure Resource Manager
    var trc = new TokenRequestContext(["https://management.azure.com/.default"]);
    TokenCredential credential = CreateTokenCredential();
    var token = await credential.GetTokenAsync(trc, cancellationToken);

    // Detect if storageKey is a SAS token; otherwise treat it as account key
    string storageKeyType = storageKey?.Contains("sig=", StringComparison.OrdinalIgnoreCase) == true || storageKey?.Contains("sv=", StringComparison.OrdinalIgnoreCase) == true
     ? "SharedAccessKey"
     : "StorageAccessKey";
    if (storageKeyType == "SharedAccessKey" && storageKey.StartsWith("?"))
    {
      storageKey = storageKey.TrimStart('?');
    }
    _logger.LogInformation("Using {KeyType} for storage auth.", storageKeyType);

    // Optional preflight: if SAS present and has read permission, HEAD the blob to confirm non-existence
    if (storageKeyType == "SharedAccessKey")
    {
      var hasRead = storageKey.Contains("sp=", StringComparison.OrdinalIgnoreCase)
        ? storageKey.Contains("r", StringComparison.Ordinal)
        : false;
      if (hasRead && !storageUri.Contains("sig=", StringComparison.OrdinalIgnoreCase))
      {
        try
        {
          var headUri = new UriBuilder(storageUri) { Query = storageKey }.Uri.ToString();
          using var head = new HttpRequestMessage(HttpMethod.Head, headUri);
          var headResp = await _httpClient.SendAsync(head, cancellationToken);
          _logger.LogInformation("Preflight HEAD status {Status} for {Uri}", (int)headResp.StatusCode, storageUri);
        }
        catch (Exception ex)
        {
          _logger.LogDebug(ex, "Preflight HEAD failed (continuing)");
        }
      }
    }

    var apiVersion = "2021-02-01-preview";
    var requestUri = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Sql/servers/{serverName}/databases/{databaseName}/export?api-version={apiVersion}";

    var body = new
    {
      storageKeyType = storageKeyType,
      storageKey = storageKey,
      storageUri = storageUri,
      administratorLogin = dbAdmin,
      administratorLoginPassword = dbPassword
    };

    using var req = new HttpRequestMessage(HttpMethod.Post, requestUri)
    {
      Content = JsonContent.Create(body)
    };
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

    _logger.LogInformation("Starting Azure SQL export to {StorageUri} for DB {Database} on server {Server}", storageUri, databaseName, serverName);

    using var resp = await _httpClient.SendAsync(req, cancellationToken);
    if (!resp.IsSuccessStatusCode)
    {
      var respBody = await resp.Content.ReadAsStringAsync(cancellationToken);
      resp.Headers.TryGetValues("x-ms-request-id", out var reqIds);
      resp.Headers.TryGetValues("x-ms-correlation-request-id", out var corrIds);
      var reqId = reqIds is not null ? string.Join(",", reqIds) : null;
      var corrId = corrIds is not null ? string.Join(",", corrIds) : null;
      var snippet = respBody?.Length >4000 ? respBody[..4000] + "..." : respBody;
      _logger.LogWarning("ARM export failed {Status} {Reason} requestId={RequestId} correlationId={CorrelationId} body={Body}", (int)resp.StatusCode, resp.ReasonPhrase, reqId, corrId, snippet);
      throw new HttpRequestException($"ARM export failed {(int)resp.StatusCode} {resp.ReasonPhrase}\nrequestUri: {requestUri}\nx-ms-request-id: {reqId}\nx-ms-correlation-request-id: {corrId}\nbody: {snippet}");
    }

    if (resp.Headers.Location is not null)
    {
      var loc = resp.Headers.Location.ToString();
      _logger.LogInformation("Azure SQL export accepted. Operation location: {Location}", loc);
      return loc;
    }

    var okBody = await resp.Content.ReadAsStringAsync(cancellationToken);
    _logger.LogInformation("Azure SQL export response: {Body}", okBody);
    return okBody;
  }

  private TokenCredential CreateTokenCredential()
  {
    var tenantId = _configuration["AzureAdTenantId"];
    var clientId = _configuration["AzureAdClientId"];
    var clientSecret = _configuration["AzureAdClientSecret"];
    if (!string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(clientId) &&
        !string.IsNullOrWhiteSpace(clientSecret))
    {
      _logger.LogInformation("Using ClientSecretCredential for AAD auth (clientId ending {Suffix})", clientId?.Length >4 ? clientId[^4..] : clientId);
      return new ClientSecretCredential(tenantId, clientId, clientSecret);
    }

    _logger.LogInformation("Using DefaultAzureCredential for AAD auth");
    return new DefaultAzureCredential();
  }
}
