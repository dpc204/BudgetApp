using System.Net.Http.Json;
using Budget.Shared.Models;
using Budget.Shared.Services;
using Microsoft.Extensions.Logging;
//using CategoryDto = Budget.Shared.Models.CategoryDto;
//using EnvelopeDto = Budget.Shared.Models.EnvelopeDto;

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

  public async Task<List<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<TransactionDto>($"transactions/{envelopeId}", cancellationToken);
    return readOnlyList;
  }

  public async Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default)
    => await GetAsync<OneTransactionDetail>($"transactions/detail/{transactionId}", cancellationToken);

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
}
