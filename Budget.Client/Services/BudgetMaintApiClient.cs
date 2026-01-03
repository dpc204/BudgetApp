namespace Budget.Client.Services;

// Uses the typed HttpClient registered in Program.cs (line 41) via AddHttpClient<IBudgetMaintApiClient, BudgetMaintApiClient>
public sealed class BudgetMaintApiClient(HttpClient http, ILogger<BudgetMaintApiClient> logger)
  : Shared.Services.IBudgetMaintApiClient
{
  // configured base address & handlers

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
  public async Task<ImportResult> ImportCategoriesAsync(string csvContent, CancellationToken cancellationToken = default)
  {
    var payload = new { csvContent };
    var result = await PostAsync<object, ImportResult>("categories/maint/import", payload, cancellationToken);
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

  // Category maintenance methods
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



  public async Task<string> ExportCategoriesAsync(CancellationToken cancellationToken = default)
  {
    using var resp = await http.GetAsync("categories/maint/export", cancellationToken);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadAsStringAsync(cancellationToken);
  }

  // Account maintenance methods
  public async Task<IEnumerable<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
  {
    logger.LogDebug("Fetching all bank accounts via BudgetMaintApiClient");
    var readOnlyList = await GetListAsync<BankAccountDto>("accounts/maint/getall", cancellationToken);
    logger.LogDebug("Fetched {Count} bank accounts", readOnlyList.Count());
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

  public async Task<BackupPlanDto> GetBackupPlanAsync(CancellationToken cancellationToken = default)
  {
    var result = await http.GetFromJsonAsync<BackupPlanDto>("/api/maintenance/backup-plan", cancellationToken);
    if (result is null)
    {
      logger.LogDebug("Null response for BackupPlanDto from /api/maintenance/backup-plan");
      throw new InvalidOperationException("Expected non-null BackupPlanDto from '/api/maintenance/backup-plan'.");
    }
    return result;
  }

  public async Task<ExportAllResponse> ExportAllTablesAsync(CancellationToken cancellationToken = default)
  {
    var result = await PostAsync<object, ExportAllResponse>("utilities/export-all", new { }, cancellationToken);
    return result;
  }

  public async Task<BackupStatusDto?> GetBackupStatusAsync(string backupId, CancellationToken cancellationToken = default)
  {
    using var resp = await http.GetAsync($"utilities/backup-status/{backupId}", cancellationToken);
    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
      return null;
    
    resp.EnsureSuccessStatusCode();
    var result = await resp.Content.ReadFromJsonAsync<BackupStatusDto>(cancellationToken: cancellationToken);
    return result;
  }

  private async Task<IEnumerable<T>> GetListAsync<T>(string relativeUrl, CancellationToken ct)
  {
    logger.LogDebug("Fetching list of {Type} from {Url}", typeof(T).Name, relativeUrl);
    var result = await http.GetFromJsonAsync<List<T>>(relativeUrl, cancellationToken: ct);
    logger.LogDebug("Fetched {Count} items of type {Type} from {Url}", result?.Count ?? 0, typeof(T).Name, relativeUrl);
    return result ?? [];
  }

  private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken ct)
  {
    var result = await http.GetFromJsonAsync<T>(relativeUrl, cancellationToken: ct);
    if(result == null)
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
