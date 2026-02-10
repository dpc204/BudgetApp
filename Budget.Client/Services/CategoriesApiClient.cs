namespace Budget.Client.Services;

/// <summary>
/// Implementation of categories API client
/// </summary>
public sealed class CategoriesApiClient(HttpClient http, ILogger<CategoriesApiClient> logger) : Shared.Services.ICategoriesApiClient
{
  // Read operations (runtime)
  public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
  {
    var readOnlyList = await GetListAsync<CategoryDto>("categories/getbyenvelopeid", cancellationToken);
    return readOnlyList;
  }

  // Maintenance operations (admin)
  public async Task<CategoryDto> AddCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default)
  {
    var payload = new { name = dto.Name, description = dto.Description, sortOrder = dto.SortOrder };
    var created = await PostAsync<object, CategoryDto>("categories/maint/Insert", payload, cancellationToken);
    return created;
  }

  public async Task<CategoryDto> UpdateCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default)
  {
    var payload = new { categoryId = dto.CategoryId, name = dto.Name, description = dto.Description, sortOrder = dto.SortOrder };
    var updated = await PutAsync<object, CategoryDto>($"categories/maint/{dto.CategoryId}", payload, cancellationToken);
    return updated;
  }

  public async Task<bool> RemoveCategoryAsync(string id, CancellationToken cancellationToken = default)
  {
    using var resp = await http.DeleteAsync($"categories/maint/{id}", cancellationToken);
    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
    resp.EnsureSuccessStatusCode();
    return true;
  }

  public async Task<ImportResult> ImportCategoriesAsync(string csvContent, CancellationToken cancellationToken = default)
  {
    var payload = new { csvContent };
    var result = await PostAsync<object, ImportResult>("categories/maint/import", payload, cancellationToken);
    return result;
  }

  public async Task<string> ExportCategoriesAsync(CancellationToken cancellationToken = default)
  {
    using var resp = await http.GetAsync("categories/maint/export", cancellationToken);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadAsStringAsync(cancellationToken);
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
