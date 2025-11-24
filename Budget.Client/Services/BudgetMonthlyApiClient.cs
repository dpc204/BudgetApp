namespace Budget.Client.Services;

/// <summary>
/// Implementation of budget monthly API client
/// </summary>
public sealed class BudgetMonthlyApiClient : Shared.Services.IBudgetMonthlyApiClient
{
  private readonly HttpClient _http;
  private readonly ILogger<BudgetMonthlyApiClient> _logger;

  public BudgetMonthlyApiClient(HttpClient http, ILogger<BudgetMonthlyApiClient> logger)
  {
    _http = http;
    _logger = logger;
  }

  public async Task<IEnumerable<BudgetMonthResponse>> GetBudgetMonthAsync(int year, int month, CancellationToken cancellationToken = default)
  {
    var result = await _http.GetFromJsonAsync<List<BudgetMonthResponse>>(
      $"budgetmonths/{year}/{month}", 
      cancellationToken);
    return result ?? [];
  }

  public async Task<CheckDraftsResponse> CheckDraftBudgetsAsync(CancellationToken cancellationToken = default)
  {
    var result = await _http.GetFromJsonAsync<CheckDraftsResponse>(
      "budgetmonths/hasdrafts", 
      cancellationToken);
    
    if (result is null)
    {
      _logger.LogDebug("Null response for CheckDraftsResponse from budgetmonths/hasdrafts");
      throw new InvalidOperationException("Expected non-null CheckDraftsResponse from 'budgetmonths/hasdrafts'.");
    }
    
    return result;
  }

  public async Task<UpdateDraftResponse> UpdateBudgetDraftAsync(int acctPeriod, int envelopeId, decimal? draftValue, CancellationToken cancellationToken = default)
  {
    var command = new { AcctPeriod = acctPeriod, EnvelopeId = envelopeId, DraftValue = draftValue };
    
    using var response = await _http.PutAsJsonAsync("budgetmonths/draft", command, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<UpdateDraftResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      _logger.LogDebug("Null response for UpdateDraftResponse from budgetmonths/draft");
      throw new InvalidOperationException("Expected non-null UpdateDraftResponse from 'budgetmonths/draft'.");
    }
    
    return result;
  }

  public async Task<ClearDraftsResponse> ClearDraftBudgetsAsync(CancellationToken cancellationToken = default)
  {
    using var response = await _http.PostAsync("budgetmonths/cleardrafts", null, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<ClearDraftsResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      _logger.LogDebug("Null response for ClearDraftsResponse from budgetmonths/cleardrafts");
      throw new InvalidOperationException("Expected non-null ClearDraftsResponse from 'budgetmonths/cleardrafts'.");
    }
    
    return result;
  }

  public async Task<ApplyDraftsResponse> ApplyDraftBudgetsAsync(CancellationToken cancellationToken = default)
  {
    using var response = await _http.PostAsync("budgetmonths/applydrafts", null, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<ApplyDraftsResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      _logger.LogDebug("Null response for ApplyDraftsResponse from budgetmonths/applydrafts");
      throw new InvalidOperationException("Expected non-null ApplyDraftsResponse from 'budgetmonths/applydrafts'.");
    }
    
    return result;
  }
}
