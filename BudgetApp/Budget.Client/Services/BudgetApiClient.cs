using System.Net.Http.Json;
using Budget.DTO; // for DTO records

namespace Budget.Client.Services;

public sealed class BudgetApiClient(HttpClient http) : Budget.DTO.IBudgetApiClient
{
  public async Task<IReadOnlyList<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default)
    => await GetAsync<EnvelopeDto>("envelopes/getall", cancellationToken);

  public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    => await GetAsync<CategoryDto>("categories/getbyenvelopeid", cancellationToken);

  public async Task<IReadOnlyList<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default)
    => await GetAsync<TransactionDto>($"transactions/{envelopeId}", cancellationToken);


  private async Task<IReadOnlyList<T>> GetAsync<T>(string relativeUrl, CancellationToken ct)
  {
    var result = await http.GetFromJsonAsync<List<T>>(relativeUrl, cancellationToken: ct);
    return result ?? [];
  }
}
