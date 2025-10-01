using System.Net.Http.Json;
using Budget.Shared.Models;
using Budget.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Budget.Client.Services;

// Uses the typed HttpClient registered in Program.cs (line 41) via AddHttpClient<IBudgetMaintApiClient, BudgetMaintApiClient>
public sealed class BudgetMaintApiClient : Shared.Services.IBudgetMaintApiClient
{
  private readonly HttpClient _http; // configured base address & handlers
  private readonly ILogger<BudgetMaintApiClient> _logger;

  public BudgetMaintApiClient(HttpClient http, ILogger<BudgetMaintApiClient> logger)
  {
    _http = http;
    _logger = logger;
  }

  public async Task<IEnumerable<EnvelopeDto>> GetEnvelopesDtoAsync(CancellationToken cancellationToken = default)
  {
    // NOTE: Server currently maps this route as POST (MapPost). If left unchanged, this GET will 405.
    // Prefer changing server to MapGet; otherwise switch to Post here.
    var readOnlyList = await GetListAsync<EnvelopeDto>("envelopes/maint/getall", cancellationToken);
    return readOnlyList;
  }

  public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<CategoryDto>("categories/getbyenvelopeid", cancellationToken);
    return readOnlyList;
  }

  public async Task<IEnumerable<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<TransactionDto>($"transactions/{envelopeId}", cancellationToken);
    return readOnlyList;
  }

  public async Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default)
    => await GetAsync<OneTransactionDetail>($"transactions/detail/{transactionId}", cancellationToken);

  // Add (insert) envelope via maintenance endpoint - return created
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

  public async Task<EnvelopeDto> UpdateAsync(EnvelopeDto dto, CancellationToken cancellationToken = default)
  {
    var payload = new
    {
      id = dto.Id,
      name = dto.Name,
      description = dto.Description,
      balance = dto.Balance,
      budget = dto.Budget,
      categoryId = dto.CategoryId,
      sortOrder = dto.SortOrder
    };
    var updated = await PutAsync<object, EnvelopeDto>($"envelopes/maint/{dto.Id}", payload, cancellationToken);
    return updated;
  }


  public async Task<bool> RemoveEnvelopeAsync(int id, CancellationToken cancellationToken = default)
  {
    using var resp = await _http.DeleteAsync($"envelopes/maint/{id}", cancellationToken);
    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
    resp.EnsureSuccessStatusCode();
    return true;
  }

  // Category maintenance methods
  public async Task<CategoryDto> AddCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default)
  {
    var payload = new { name = dto.Name, description = dto.Description, sortOrder = dto.SortOrder };
    var created = await PostAsync<object, CategoryDto>("categories/maint/Insert", payload, cancellationToken);
    return created;
  }

  public async Task<CategoryDto> UpdateCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default)
  {
    var payload = new { id = dto.Id, name = dto.Name, description = dto.Description, sortOrder = dto.SortOrder };
    var updated = await PutAsync<object, CategoryDto>($"categories/maint/{dto.Id}", payload, cancellationToken);
    return updated;
  }

  public async Task<bool> RemoveCategoryAsync(int id, CancellationToken cancellationToken = default)
  {
    using var resp = await _http.DeleteAsync($"categories/maint/{id}", cancellationToken);
    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
    resp.EnsureSuccessStatusCode();
    return true;
  }

  private async Task<IEnumerable<T>> GetListAsync<T>(string relativeUrl, CancellationToken ct)
  {
    var result = await _http.GetFromJsonAsync<List<T>>(relativeUrl, cancellationToken: ct);
    return result ?? [];
  }

  private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken ct)
  {
    var result = await _http.GetFromJsonAsync<T>(relativeUrl, cancellationToken: ct);
    if(result == null)
    {
      _logger.LogDebug("Null response for {Type} from {Url}", typeof(T).Name, relativeUrl);
      throw new InvalidOperationException($"Expected non-null {typeof(T).Name} from '{relativeUrl}'.");
    }
    return result!;
  }

  private async Task<TResponse> PostAsync<TRequest, TResponse>(string relativeUrl, TRequest payload, CancellationToken ct)
  {
    using var resp = await _http.PostAsJsonAsync(relativeUrl, payload, ct);
    resp.EnsureSuccessStatusCode();
    var result = await resp.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
    if (result is null)
    {
      _logger.LogDebug("Null response for {Type} from {Url}", typeof(TResponse).Name, relativeUrl);
      throw new InvalidOperationException($"Expected non-null {typeof(TResponse).Name} from '{relativeUrl}'.");
    }
    return result;
  }

  private async Task<TResponse> PutAsync<TRequest, TResponse>(string relativeUrl, TRequest payload, CancellationToken ct)
  {
    using var resp = await _http.PutAsJsonAsync(relativeUrl, payload, ct);
    resp.EnsureSuccessStatusCode();
    var result = await resp.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
    if (result is null)
    {
      _logger.LogDebug("Null response for {Type} from {Url}", typeof(TResponse).Name, relativeUrl);
      throw new InvalidOperationException($"Expected non-null {typeof(TResponse).Name} from '{relativeUrl}'.");
    }
    return result;
  }
}
