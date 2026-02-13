namespace Budget.Client.Services.ApiClients;

/// <summary>
/// Implementation of envelope API client
/// </summary>
public sealed class EnvelopesApiClient(HttpClient http, ILogger<EnvelopesApiClient> logger) : IEnvelopesApiClient
{
  // Read operations (runtime)
  public async Task<List<EnvelopeDto>> GetEnvelopesAsync(EnvelopeTypes envelopeType = EnvelopeTypes.All,
    CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<EnvelopeDto>($"envelopes/getall/{envelopeType}", cancellationToken);
    return readOnlyList;
  }

  public async Task<EnvelopeDto> GetEnvelopeByIdAsync(int envelopeId, CancellationToken cancellationToken = default)
    => await GetAsync<EnvelopeDto>($"envelopes/{envelopeId}", cancellationToken);

  public async Task<EnvelopeDto> GetEnvelopeByEnvelopeTypeAsync(EnvelopeTypes envType, CancellationToken cancellationToken = default)
  {
    var env = await GetAsync<EnvelopeDto>($"envelopes/bytype/{envType}", cancellationToken);
    return env;
  }

  // Fund operations (budget planning)
  public async Task<FBResult<int>> FundEnvelopesAsync(CancellationToken cancellationToken)
  {
    using var response = await http.PostAsync("envelopes/fund", null, cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
      logger.LogWarning("FundEnvelopes failed with status {Status}", response.StatusCode);
      return FBResult<int>.Failure( "Failed to fund envelopes");
    }

    try
    {
      var successResponse = await response.Content.ReadFromJsonAsync<FundSuccessResponse>(cancellationToken);
      if (successResponse?.FundedCount != null)
      {
        return FBResult<int>.Success(successResponse.FundedCount);
      }

      var errorResponse = await response.Content.ReadFromJsonAsync<FundErrorResponse>(cancellationToken);
      //if (errorResponse?.Error != null)
      //{
      //  logger.LogWarning("FundEnvelopes failed: {Error}", errorResponse.Error);
      //  return new FundEnvelopesResponse(false, errorResponse.Error, 0);
      //}

      //logger.LogWarning("FundEnvelopes returned unexpected response format");
      //return new FundEnvelopesResponse(false, "Unexpected response format", 0);

      throw new ArgumentException("");	
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error deserializing FundEnvelopes response");
      return FBResult<int>.Failure(ex.Message); //new FundEnvelopesResponse(false, ex.Message, 0);
    }

    ;
  }

  public async Task<UpdateFundAmountResponse> UpdateFundAmountAsync(int envelopeId, decimal? fundAmount, CancellationToken cancellationToken = default)
  {
    var command = new { EnvelopeId = envelopeId, FundAmount = fundAmount };

    using var response = await http.PutAsJsonAsync("envelopes/fundamount", command, cancellationToken);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<UpdateFundAmountResponse>(cancellationToken: cancellationToken);

    if (result is null)
    {
      logger.LogDebug("Null response for UpdateFundAmountResponse from envelopes/fundamount");
      throw new InvalidOperationException("Expected non-null UpdateFundAmountResponse from 'envelopes/fundamount'.");
    }

    return result;
  }

  public async Task<ClearAllFundAmountsResponse> ClearAllFundAmountsAsync(CancellationToken cancellationToken = default)
  {
    using var response = await http.PostAsync("envelopes/clearallfundamounts", null, cancellationToken);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<ClearAllFundAmountsResponse>(cancellationToken: cancellationToken);

    if (result is null)
    {
      logger.LogDebug("Null response for ClearAllFundAmountsResponse from envelopes/clearallfundamounts");
      throw new InvalidOperationException("Expected non-null ClearAllFundAmountsResponse from 'envelopes/clearallfundamounts'.");
    }

    return result;
  }

  // Maintenance operations (admin)
  public async Task<IEnumerable<EnvelopeDto>> GetEnvelopesDtoAsync(CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<EnvelopeDto>("envelopes/maint/getall", cancellationToken);
    return readOnlyList;
  }

  public async Task<EnvelopeDto> AddAsync(EnvelopeDto dto)
  {
    var payload = new
    {
      name = dto.Name,
      description = dto.Description,
      balance = dto.Balance,
      budget = dto.Budget,
      categoryId = dto.CategoryId,
      sortOrder = dto.SortOrder
    };

    var created = await PostAsync<object, EnvelopeDto>("envelopes/maint/Insert", payload, CancellationToken.None);
    return created;
  }

  public async Task<EnvelopeUpdateDto> UpdateAsync(EnvelopeUpdateDto dto, CancellationToken cancellationToken = default)
  {
    var updated = await PutAsync<object, EnvelopeUpdateDto>($"envelopes/maint/{dto.Id}", dto, cancellationToken);
    return updated;
  }

  public async Task<bool> RemoveEnvelopeAsync(int id, CancellationToken cancellationToken = default)
  {
    using var resp = await http.DeleteAsync($"envelopes/maint/{id}", cancellationToken);
    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
    resp.EnsureSuccessStatusCode();
    return true;
  }

  public async Task<ImportResult> ImportEnvelopesAsync(string csvContent, CancellationToken cancellationToken = default)
  {
    var payload = new { csvContent };
    var result = await PostAsync<object, ImportResult>("envelopes/maint/import", payload, cancellationToken);
    return result;
  }

  public async Task<string> ExportEnvelopesAsync(CancellationToken cancellationToken = default)
  {
    using var resp = await http.GetAsync("envelopes/maint/export", cancellationToken);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadAsStringAsync(cancellationToken);
  }

  public async Task<int> GetEnvelopeTransactionCountAsync(int envelopeId, CancellationToken cancellationToken = default)
  {
    var response = await http.GetFromJsonAsync<EnvelopeTransactionCountResponse>($"envelopes/maint/{envelopeId}/transaction-count", cancellationToken);
    return response?.TransactionCount ?? 0;
  }

  // Helper methods
  private async Task<List<T>> GetListAsync<T>(string relativeUrl, CancellationToken ct)
  {
    var result = await http.GetFromJsonAsync<List<T>>(relativeUrl, cancellationToken: ct);
    return result ?? [];
  }

  private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken ct)
  {
    var result = await http.GetFromJsonAsync<T>(relativeUrl, cancellationToken: ct);
    if (result == null)
    {
      logger.LogDebug("Null response for {Type} from {Url}", typeof(T).Name, relativeUrl);
      throw new InvalidOperationException($"Expected non-null {typeof(T).Name} from '{relativeUrl}'.");
    }

    return result!;
  }

  private async Task<TResponse> PostAsync<TRequest, TResponse>(string relativeUrl, TRequest payload, CancellationToken ct)
  {
    using var resp = await http.PostAsJsonAsync(relativeUrl, payload, ct);
    resp.EnsureSuccessStatusCode();
    var result = await resp.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
    if (result is null)
    {
      logger.LogDebug("Null response for {Type} from {Url}", typeof(TResponse).Name, relativeUrl);
      throw new InvalidOperationException($"Expected non-null {typeof(TResponse).Name} from '{relativeUrl}'.");
    }
    return result;
  }

  private async Task<TResponse> PutAsync<TRequest, TResponse>(string relativeUrl, TRequest payload, CancellationToken ct)
  {
    using var resp = await http.PutAsJsonAsync(relativeUrl, payload, ct);
    resp.EnsureSuccessStatusCode();
    var result = await resp.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
    if (result is null)
    {
      logger.LogDebug("Null response for {Type} from {Url}", typeof(TResponse).Name, relativeUrl);
      throw new InvalidOperationException($"Expected non-null {typeof(TResponse).Name} from '{relativeUrl}'.");
    }
    return result;
  }

  private sealed record EnvelopeTransactionCountResponse(int EnvelopeId, int TransactionCount);
}

file record FundSuccessResponse(int FundedCount);
file record FundErrorResponse(string Error);
