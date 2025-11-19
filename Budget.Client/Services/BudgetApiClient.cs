using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

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

  public async Task<List<TransactionDto>> GetTransactionsUnallocatedAsync(CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<TransactionDto>($"transactions/unallocated", cancellationToken);
    return readOnlyList;
  }

  public async Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default)
    => await GetAsync<OneTransactionDetail>($"transactions/detail/{transactionId}", cancellationToken);

  public async Task<List<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
    => await GetListAsync<BankAccountDto>($"accounts/maint/getall", cancellationToken);

  public async Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken cancellationToken = default)
    => await http.GetFromJsonAsync<UserInfoDto>("api/auth/userinfo", cancellationToken);

  public async Task<string> TriggerAzureSqlBackupAsync(CancellationToken cancellationToken = default)
  {
    using var resp = await http.PostAsync("/api/maintenance/backup-azure-sql", null, cancellationToken);
    var body = await resp.Content.ReadAsStringAsync(cancellationToken);
    if (!resp.IsSuccessStatusCode)
    {
      throw new InvalidOperationException($"Backup failed ({(int)resp.StatusCode}): {body}");
    }
    return body;
  }

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

  public async Task<bool> AllocateTransactionAsync(int transactionId, int lineId, int envelopeId, string description, CancellationToken cancellationToken = default)
  {
    var payload = new { TransactionId = transactionId, LineId = lineId, EnvelopeId = envelopeId, Description = description };
    using var resp = await http.PutAsJsonAsync("/transactions/allocate", payload, cancellationToken);
    return resp.IsSuccessStatusCode;
  }
}