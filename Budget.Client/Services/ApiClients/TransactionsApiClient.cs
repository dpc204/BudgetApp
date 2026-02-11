using Budget.Shared.Models.Queries;

namespace Budget.Client.Services.ApiClients;

/// <summary>
/// Implementation of transactions API client
/// </summary>
public sealed class TransactionsApiClient(HttpClient http, ILogger<TransactionsApiClient> logger) : ITransactionsApiClient
{
  // Read operations
  public async Task<List<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, int startIndex = 0,
    int pageSize = 0, CancellationToken cancellationToken = default)
  {
    var parameters = "";
    if (startIndex > 0 && pageSize > 0)
      parameters = $"?startIndex={startIndex}&pageSize={pageSize}";

    var readOnlyList = await GetListAsync<TransactionDto>($"transactions/{envelopeId}{parameters}", cancellationToken);
    return readOnlyList;
  }

  public async Task<Result<List<EnvelopeTransactionListItem>>> GetFullTransactionsByEnvelopeAsync(int envelopeId,
    int startIndex = 0, int pageSize = 0, CancellationToken cancellationToken = default)
  {
    try
    {
      var parameters = "";
      if (startIndex > 0 && pageSize > 0)
        parameters = $"?startIndex={startIndex}&pageSize={pageSize}";

      var response = await http.GetAsync($"transactions/getfull/{envelopeId}{parameters}", cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("GetFullTransactionsByEnvelopeAsync failed with status {Status}: {Error}",
          response.StatusCode, errorContent);
        return Result.Fail<List<EnvelopeTransactionListItem>>(
          $"Failed to get transactions: {response.StatusCode}");
      }

      var result =
        await response.Content.ReadFromJsonAsync<List<EnvelopeTransactionListItem>>(
          cancellationToken: cancellationToken);

      if (result != null)
      {
        return Result.Ok(result);
      }
      else
      {
        logger.LogWarning("GetFullTransactionsByEnvelopeAsync returned null content.");
        return Result.Fail<List<EnvelopeTransactionListItem>>("Received null data from the API.");
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting transactions");
      return Result.Fail<List<EnvelopeTransactionListItem>>($"Error: {ex.Message}");
    }
  }

  public async Task<Result<List<TransactionDto>>> GetTransactionsUnassignedAsync(
    CancellationToken cancellationToken = default)
  {
    try
    {
      var response = await http.GetAsync($"transactions/unassigned", cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("GetTransactionsUnassignedAsync failed with status {Status}: {Error}",
          response.StatusCode, errorContent);
        return Result.Fail<List<TransactionDto>>($"Failed to get unassigned transactions: {response.StatusCode}");
      }

      var result = await response.Content.ReadFromJsonAsync<List<TransactionDto>>(cancellationToken: cancellationToken);
      return Result.Ok(result ?? []);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting unassigned transactions");
      return Result.Fail<List<TransactionDto>>($"Error: {ex.Message}");
    }
  }

  public async Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId,
    CancellationToken cancellationToken = default)
  {
    return await GetAsync<OneTransactionDetail>($"transactions/detail/{transactionId}", cancellationToken);
  }

  public async Task<AssignQueryResult> GetUnassignedVirtualAsync(AssignQuery query,
    CancellationToken cancellationToken = default)
  {
    var response = await http.PostAsJsonAsync("transactions/unassigned/virtual", query, cancellationToken);
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<AssignQueryResult>(cancellationToken: cancellationToken);
    return result ?? new AssignQueryResult();
  }

  // Write operations
  public async Task<TransactionAddResult> AddTransactionAsync(OneTransactionDetail newTransaction,
    CancellationToken cancellationToken = default)
  {
    var payload = new { Trans = newTransaction };

    using var resp = await http.PostAsJsonAsync("/Transaction/Insert", payload, cancellationToken);
    resp.EnsureSuccessStatusCode();

    try
    {
      var transaction =
        await resp.Content.ReadFromJsonAsync<TransactionAddResult>(cancellationToken: cancellationToken);
      return transaction ?? new TransactionAddResult();
    }
    catch (Exception ex)
    {
      logger.LogDebug(ex, "No response body or invalid JSON for AddTransaction at {Url}", "/Transaction/Insert");
      return new TransactionAddResult();
    }
  }

  public async Task<TransactionAddResult> AddMultipleTransactionsAsync(List<OneTransactionDetail> newTransaction,
    CancellationToken cancellationToken = default)
  {
    var payload = new { Trans = newTransaction };

    using var resp = await http.PostAsJsonAsync("/Transaction/InsertMulti", payload, cancellationToken);
    resp.EnsureSuccessStatusCode();

    try
    {
      var transaction =
        await resp.Content.ReadFromJsonAsync<TransactionAddResult>(cancellationToken: cancellationToken);
      return transaction ?? new TransactionAddResult();
    }
    catch (Exception ex)
    {
      logger.LogDebug(ex, "No response body or invalid JSON for AddMultipleTransactions at {Url}", "/Transaction/InsertMulti");
      return new TransactionAddResult();
    }
  }

  public async Task<List<EnvelopeDto>> UpdateTransactionAsync(OneTransactionDetail transaction,
    CancellationToken cancellationToken = default)
  {
    var payload = new { Trans = transaction };

    using var resp = await http.PutAsJsonAsync("/Transaction/Update", payload, cancellationToken);

    if (!resp.IsSuccessStatusCode)
    {
      logger.LogWarning("UpdateTransaction failed with status {Status} for transaction {Id}",
        resp.StatusCode, transaction.Id);
      return [];
    }

    try
    {
      var result =
        await resp.Content.ReadFromJsonAsync<List<EnvelopeDto>>(cancellationToken: cancellationToken);

      if (result != null)
      {
        return result;
      }
    }
    catch (Exception ex)
    {
      logger.LogDebug(ex, "No response body or invalid JSON for UpdateTransaction at {Url}", "/Transaction/Update");
    }

    return [];
  }

  public async Task<List<EnvelopeDto>> VoidTransactionAsync(int transactionId,
    CancellationToken cancellationToken = default)
  {
    var payload = new { TransactionId = transactionId };

    using var resp = await http.PostAsJsonAsync("/Transaction/Void", payload, cancellationToken);

    if (!resp.IsSuccessStatusCode)
    {
      logger.LogWarning("VoidTransaction failed with status {Status} for transaction {Id}",
        resp.StatusCode, transactionId);
      return [];
    }

    try
    {
      var result =
        await resp.Content.ReadFromJsonAsync<Result<List<EnvelopeDto>>>(cancellationToken: cancellationToken);

      if (result?.IsSuccess == true)
      {
        return result.Value ?? [];
      }

      logger.LogWarning("VoidTransaction failed: {Error}", result?.Errors);
      return [];
    }
    catch (Exception ex)
    {
      logger.LogDebug(ex, "No response body or invalid JSON for VoidTransaction at {Url}", "/Transaction/Void");
      return [];
    }
  }

  public async Task<bool> AssignTransactionAsync(int transactionId, int lineId, int envelopeId, string description,
    CancellationToken cancellationToken = default)
  {
    var payload = new
      { TransactionId = transactionId, LineId = lineId, EnvelopeId = envelopeId, Description = description };
    using var resp = await http.PutAsJsonAsync("/transactions/assign", payload, cancellationToken);
    return resp.IsSuccessStatusCode;
  }

  // Import/Export operations
  public async Task<int> ImportTransactionsAsync(List<TransactionImportDto> transactions,
    CancellationToken cancellationToken = default)
  {
    var payload = new { Transactions = transactions };
    using var resp = await http.PostAsJsonAsync("/Transaction/Import", payload, cancellationToken);
    resp.EnsureSuccessStatusCode();
    var result = await resp.Content.ReadFromJsonAsync<ImportResult>(cancellationToken);
    return result?.Count ?? 0;
  }

  public async Task<List<TransactionImportDto>> GetTransactionImportsAsync(
    CancellationToken cancellationToken = default)
    => await GetListAsync<TransactionImportDto>("/Transaction/Import", cancellationToken);

  public async Task<int> ClearTransactionImportsAsync(CancellationToken cancellationToken = default)
  {
    using var resp = await http.DeleteAsync("/Transaction/Import", cancellationToken);
    resp.EnsureSuccessStatusCode();
    var result = await resp.Content.ReadFromJsonAsync<ImportResult>(cancellationToken);
    return result?.Count ?? 0;
  }

  public async Task<bool> UpdateTransactionImportAsync(int id, bool duplicate, bool keepDuplicate,
    CancellationToken cancellationToken = default)
  {
    var payload = new { Duplicate = duplicate, KeepDuplicate = keepDuplicate };
    using var resp = await http.PutAsJsonAsync($"/Transaction/Import/{id}", payload, cancellationToken);
    return resp.IsSuccessStatusCode;
  }

  public async Task<int> UpdateTransactionImportsBatchAsync(List<int> ids, bool duplicate,
    CancellationToken cancellationToken = default)
  {
    var payload = new { Ids = ids, Duplicate = duplicate };
    using var resp = await http.PutAsJsonAsync("/Transaction/Import/Batch", payload, cancellationToken);
    resp.EnsureSuccessStatusCode();
    var result = await resp.Content.ReadFromJsonAsync<BatchUpdateResult>(cancellationToken);
    return result?.UpdatedCount ?? 0;
  }

  public async Task<int> LoadTransactionImportsToUnassignedAsync(int accountId, int userId,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var command = new LoadImportsCommand(accountId, userId);
      using var resp = await http.PostAsJsonAsync("/api/transactions/load-imports", command, cancellationToken);

      if (!resp.IsSuccessStatusCode)
      {
        logger.LogWarning("LoadTransactionImportsToUnassigned failed with status {Status}", resp.StatusCode);
        return 0;
      }

      var result = await resp.Content.ReadFromJsonAsync<LoadImportsResponse>(cancellationToken: cancellationToken);
      return result?.ImportedCount ?? 0;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error loading transaction imports to unassigned");
      return 0;
    }
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

  private sealed record ImportResult(int Count);
  private sealed record BatchUpdateResult(int UpdatedCount);
  private sealed record LoadImportsCommand(int AccountId, int UserId);
  private sealed record LoadImportsResponse(int ImportedCount);
}
