using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;

namespace Budget.Client.Services;

public sealed class BudgetApiClient(HttpClient http, ILogger<BudgetApiClient> logger) : Shared.Services.IBudgetApiClient
{
  public async Task<List<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<EnvelopeDto>("envelopes/getall", cancellationToken);
    return readOnlyList;
  }

  public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<CategoryDto>("categories/getbyenvelopeid", cancellationToken);
    return readOnlyList;
  }

  public async Task<List<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId,
    CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<TransactionDto>($"transactions/{envelopeId}", cancellationToken);
    return readOnlyList;
  }

  public async Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default)
    => await GetAsync<OneTransactionDetail>($"transactions/detail/{transactionId}", cancellationToken);

  public async Task<List<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
    => await GetListAsync<BankAccountDto>($"accounts/maint/getall", cancellationToken);


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

  public async Task<List<EnvelopeDto>> AddTransactionAsync(OneTransactionDetail newTransaction,
    CancellationToken cancellationToken = default)
  {
    // The API currently returns 202 Accepted with no body. Post and ensure success; if no JSON body, return the request object.
    var payload = new { Trans = newTransaction };

    using var resp = await http.PostAsJsonAsync("/Transaction/Insert", payload, cancellationToken);
    resp.EnsureSuccessStatusCode();

    try
    {
      var envelopes = await resp.Content.ReadFromJsonAsync<List<EnvelopeDto>>(cancellationToken: cancellationToken);
      return envelopes ?? [];
    }
    catch (Exception ex)
    {
      // Log at debug level and return the submitted transaction to maintain API contract
      logger.LogDebug(ex, "No response body or invalid JSON for AddTransaction at {Url}", "/Transaction/Insert");
      return [];
    }
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

  public async Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken cancellationToken = default)
  {
    return await http.GetFromJsonAsync<UserInfoDto>("api/auth/userinfo", cancellationToken);
  }
}