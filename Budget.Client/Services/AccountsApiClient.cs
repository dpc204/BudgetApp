namespace Budget.Client.Services;

/// <summary>
/// Implementation of accounts API client
/// </summary>
public sealed class AccountsApiClient(HttpClient http, ILogger<AccountsApiClient> logger) : Shared.Services.IAccountsApiClient
{
  public async Task<List<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
  {
    logger.LogDebug("Fetching all bank accounts via AccountsApiClient");
    var readOnlyList = await GetListAsync<BankAccountDto>("accounts/maint/getall", cancellationToken);
    logger.LogDebug("Fetched {Count} bank accounts", readOnlyList.Count);
    return readOnlyList;
  }

  public async Task<BankAccountDto> AddAccountAsync(BankAccountDto dto, CancellationToken cancellationToken = default)
  {
    var payload = new { name = dto.Name, balance = dto.Balance, accountType = dto.AccountType };
    var created = await PostAsync<object, BankAccountDto>("accounts/maint/Insert", payload, cancellationToken);
    return created;
  }

  public async Task<BankAccountDto> UpdateAccountAsync(BankAccountDto dto, CancellationToken cancellationToken = default)
  {
    var payload = new { id = dto.Id, name = dto.Name, balance = dto.Balance, accountType = dto.AccountType };
    var updated = await PutAsync<object, BankAccountDto>($"accounts/maint/{dto.Id}", payload, cancellationToken);
    return updated;
  }

  public async Task<bool> RemoveAccountAsync(int id, CancellationToken cancellationToken = default)
  {
    using var resp = await http.DeleteAsync($"accounts/maint/{id}", cancellationToken);
    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
    resp.EnsureSuccessStatusCode();
    return true;
  }

  // Helper methods
  private async Task<List<T>> GetListAsync<T>(string relativeUrl, CancellationToken ct)
  {
    var result = await http.GetFromJsonAsync<List<T>>(relativeUrl, cancellationToken: ct);
    return result ?? [];
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
}
