namespace Budget.Client.Services;

/// <summary>
/// Implementation of budget monthly API client
/// </summary>
public sealed class BudgetMonthlyApiClient(HttpClient http, ILogger<BudgetMonthlyApiClient> logger)
  : Shared.Services.IBudgetMonthlyApiClient
{
  public async Task<IEnumerable<BudgetMonthResponse>> GetBudgetMonthAsync(int year, int month, CancellationToken cancellationToken = default)
  {
    var result = await http.GetFromJsonAsync<List<BudgetMonthResponse>>(
      $"budgetmonths/{year}/{month}", 
      cancellationToken);
    return result ?? [];
  }

  public async Task<CheckDraftsResponse> CheckDraftBudgetsAsync(CancellationToken cancellationToken = default)
  {
    var result = await http.GetFromJsonAsync<CheckDraftsResponse>(
      "budgetmonths/hasdrafts", 
      cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for CheckDraftsResponse from budgetmonths/hasdrafts");
      throw new InvalidOperationException("Expected non-null CheckDraftsResponse from 'budgetmonths/hasdrafts'.");
    }
    
    return result;
  }

  public async Task<UpdateDraftResponse> UpdateBudgetDraftAsync(int acctPeriod, int envelopeId, decimal? draftValue, CancellationToken cancellationToken = default)
  {
    var command = new { AcctPeriod = acctPeriod, EnvelopeId = envelopeId, DraftValue = draftValue };
    
    using var response = await http.PutAsJsonAsync("budgetmonths/draft", command, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<UpdateDraftResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for UpdateDraftResponse from budgetmonths/draft");
      throw new InvalidOperationException("Expected non-null UpdateDraftResponse from 'budgetmonths/draft'.");
    }
    
    return result;
  }

  public async Task<ClearDraftsResponse> ClearDraftBudgetsAsync(CancellationToken cancellationToken = default)
  {
    using var response = await http.PostAsync("budgetmonths/cleardrafts", null, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<ClearDraftsResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for ClearDraftsResponse from budgetmonths/cleardrafts");
      throw new InvalidOperationException("Expected non-null ClearDraftsResponse from 'budgetmonths/cleardrafts'.");
    }
    
    return result;
  }

  /// <summary>
  /// Applies pending budget drafts on the server and returns the operation result.
  /// </summary>
  /// <param name="cancellationToken">Token to cancel the HTTP request.</param>
  /// <returns>An <see cref="ApplyDraftsResponse"/> describing the outcome of applying drafts.</returns>
  /// <exception cref="InvalidOperationException">Thrown when the server response body is unexpectedly null.</exception>
  public async Task<ApplyDraftsResponse> ApplyDraftBudgetsAsync(CancellationToken cancellationToken = default)
  {
    using var response = await http.PostAsync("budgetmonths/applydrafts", null, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<ApplyDraftsResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for ApplyDraftsResponse from budgetmonths/applydrafts");
      throw new InvalidOperationException("Expected non-null ApplyDraftsResponse from 'budgetmonths/applydrafts'.");
    }
    
    return result;
  }

  /// <summary>
  /// Copies budget data for the specified accounting period to the next month without confirming overwrites.
  /// </summary>
  /// <param name="sourceAcctPeriod">The source accounting period to copy from.</param>
  /// <param name="copyFromDraft">If `true`, copy values from draft budgets; otherwise copy from published budgets.</param>
  /// <returns>A <see cref="CopyBudgetToNextMonthResponse"/> describing the outcome of the copy operation.</returns>
  public async Task<CopyBudgetToNextMonthResponse> CopyBudgetToNextMonthAsync(int sourceAcctPeriod, bool copyFromDraft, CancellationToken cancellationToken = default)
  {
    return await CopyBudgetToNextMonthAsync(sourceAcctPeriod, copyFromDraft, false, cancellationToken);
  }

  /// <summary>
  /// Copies budget data from the specified accounting period to the next month.
  /// </summary>
  /// <param name="sourceAcctPeriod">The accounting period to copy budget data from.</param>
  /// <param name="copyFromDraft">If true, copy values from draft budgets; otherwise copy from applied budgets.</param>
  /// <param name="confirmOverwrite">If true, allow overwriting existing budget data in the destination period.</param>
  /// <param name="cancellationToken">Token to cancel the operation.</param>
  /// <returns>A <see cref="CopyBudgetToNextMonthResponse"/> describing the result of the copy operation.</returns>
  /// <exception cref="InvalidOperationException">Thrown when the API returns a null <see cref="CopyBudgetToNextMonthResponse"/>.</exception>
  public async Task<CopyBudgetToNextMonthResponse> CopyBudgetToNextMonthAsync(int sourceAcctPeriod, bool copyFromDraft, bool confirmOverwrite, CancellationToken cancellationToken = default)
  {
    var command = new { SourceAcctPeriod = sourceAcctPeriod, CopyFromDraft = copyFromDraft, ConfirmOverwrite = confirmOverwrite };
    
    using var response = await http.PostAsJsonAsync("budgetmonths/copytonextmonth", command, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<CopyBudgetToNextMonthResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for CopyBudgetToNextMonthResponse from budgetmonths/copytonextmonth");
      throw new InvalidOperationException("Expected non-null CopyBudgetToNextMonthResponse from 'budgetmonths/copytonextmonth'.");
    }
    
    return result;
  }
}