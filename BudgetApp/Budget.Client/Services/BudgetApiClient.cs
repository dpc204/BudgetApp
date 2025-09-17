using System.Net.Http.Json;
using Budget.DTO; // for DTO records
using Microsoft.Extensions.Logging;

namespace Budget.Client.Services;

public sealed class BudgetApiClient(HttpClient http, ILogger<BudgetApiClient> logger) : Budget.DTO.IBudgetApiClient
{
  public async Task<IReadOnlyList<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default)
    => await GetListAsync<EnvelopeDto>("envelopes/getall", cancellationToken);

  public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    => await GetListAsync<CategoryDto>("categories/getbyenvelopeid", cancellationToken);

  public async Task<IReadOnlyList<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default)
    => await GetListAsync<TransactionDto>($"transactions/{envelopeId}", cancellationToken);

  public async Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default)
    => await GetAsync<OneTransactionDetail>($"transactions/detail/{transactionId}", cancellationToken);

  private async Task<IReadOnlyList<T>> GetListAsync<T>(string relativeUrl, CancellationToken ct)
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
}
