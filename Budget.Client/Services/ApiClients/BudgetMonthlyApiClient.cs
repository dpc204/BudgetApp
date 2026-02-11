using Mapster;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Budget.Client.Services.ApiClients;

/// <summary>
/// Implementation of budget monthly API client
/// </summary>
public sealed class BudgetMonthlyApiClient(HttpClient http, ILogger<BudgetMonthlyApiClient> logger)
  : IBudgetMonthlyApiClient
{
  public async Task<IEnumerable<BudgetMonthResponse>> GetBudgetMonthAsync(int year, int month, CancellationToken cancellationToken = default)
  {
    var result = await http.GetFromJsonAsync<List<BudgetMonthResponse>>(
      $"budgetmonths/{year}/{month}", 
      cancellationToken);
    return result ?? [];
  }

  public async Task<EnvelopeDto> GetEnvelopeByEnvelopeTypeAsync(EnvelopeTypes envType, CancellationToken cancellationToken = default)
  {
    var env = await GetAsync<EnvelopeDto>($"envelopes/bytype/{envType}", cancellationToken);
    return env;
  }

  public async Task<FundEnvelopesResponse> FundEnvelopesAsync(CancellationToken cancellationToken)
  {
    using var response = await http.PostAsync("envelopes/fund", null, cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
      logger.LogWarning("FundEnvelopes failed with status {Status}", response.StatusCode);
      return new FundEnvelopesResponse(false, "Failed to fund envelopes", 0);
    }

    try
    {
      // Try to deserialize success response with fundedCount
      var successResponse = await response.Content.ReadFromJsonAsync<FundSuccessResponse>(cancellationToken);
      if (successResponse?.FundedCount != null)
      {
        return new FundEnvelopesResponse(
          true, 
          $"Successfully funded {successResponse.FundedCount} envelopes", 
          successResponse.FundedCount);
      }

      // Try to deserialize error response
      var errorResponse = await response.Content.ReadFromJsonAsync<FundErrorResponse>(cancellationToken);
      if (errorResponse?.Error != null)
      {
        logger.LogWarning("FundEnvelopes failed: {Error}", errorResponse.Error);
        return new FundEnvelopesResponse(false, errorResponse.Error, 0);
      }

      logger.LogWarning("FundEnvelopes returned unexpected response format");
      return new FundEnvelopesResponse(false, "Unexpected response format", 0);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error deserializing FundEnvelopes response");
      return new FundEnvelopesResponse(false, ex.Message, 0);
    }
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

  public async Task<UpdateLockResponse> UpdateBudgetLockAsync(int acctPeriod, int envelopeId, bool isLocked, CancellationToken cancellationToken = default)
  {
    var command = new { AcctPeriod = acctPeriod, EnvelopeId = envelopeId, IsLocked = isLocked };
    
    using var response = await http.PutAsJsonAsync("budgetmonths/lock", command, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<UpdateLockResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for UpdateLockResponse from budgetmonths/lock");
      throw new InvalidOperationException("Expected non-null UpdateLockResponse from 'budgetmonths/lock'.");
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

  public async Task<ApplyDraftsResponse> ApplyDraftValuesToBudgetAsync(CancellationToken cancellationToken = default)
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

  public async Task<CopyBudgetToNextMonthResponse> CopyBudgetToNextMonthAsync(int sourceAcctPeriod, bool copyFromDraft, CancellationToken cancellationToken = default)
  {
    return await CopyBudgetToNextMonthAsync(sourceAcctPeriod, copyFromDraft, false, cancellationToken);
  }

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

  public async Task<ClearMonthBudgetsResponse> ClearMonthBudgetsAsync(int acctPeriod, CancellationToken cancellationToken = default)
  {
    var command = new { AcctPeriod = acctPeriod };
    
    using var response = await http.PostAsJsonAsync("budgetmonths/clearmonthbudgets", command, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<ClearMonthBudgetsResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for ClearMonthBudgetsResponse from budgetmonths/clearmonthbudgets");
      throw new InvalidOperationException("Expected non-null ClearMonthBudgetsResponse from 'budgetmonths/clearmonthbudgets'.");
    }
    
    return result;
  }

  public async Task<ClearMonthDraftsResponse> ClearMonthDraftsAsync(int acctPeriod, CancellationToken cancellationToken = default)
  {
    var command = new { AcctPeriod = acctPeriod };
    
    using var response = await http.PostAsJsonAsync("budgetmonths/clearmonthdrafts", command, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<ClearMonthDraftsResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for ClearMonthDraftsResponse from budgetmonths/clearmonthdrafts");
      throw new InvalidOperationException("Expected non-null ClearMonthDraftsResponse from 'budgetmonths/clearmonthdrafts'.");
    }
    
    return result;
  }

  public async Task<ClearMonthBothResponse> ClearMonthBothAsync(int acctPeriod, CancellationToken cancellationToken = default)
  {
    var command = new { AcctPeriod = acctPeriod };
    
    using var response = await http.PostAsJsonAsync("budgetmonths/clearmonthboth", command, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<ClearMonthBothResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for ClearMonthBothResponse from budgetmonths/clearmonthboth");
      throw new InvalidOperationException("Expected non-null ClearMonthBothResponse from 'budgetmonths/clearmonthboth'.");
    }
    
    return result;
  }

  public async Task<ApplyMonthDraftsResponse> ApplyMonthDraftsAsync(int acctPeriod, CancellationToken cancellationToken = default)
  {
    var command = new { AcctPeriod = acctPeriod };
    
    using var response = await http.PostAsJsonAsync("budgetmonths/applymonthdrafts", command, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<ApplyMonthDraftsResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for ApplyMonthDraftsResponse from budgetmonths/applymonthdrafts");
      throw new InvalidOperationException("Expected non-null ApplyMonthDraftsResponse from 'budgetmonths/applymonthdrafts'.");
    }
    
    return result;
  }

  public async Task<UpdateFundAmountResponse> UpdateFundAmountAsync(int envelopeId, decimal? fundAmount, CancellationToken cancellationToken = default)
  {
    var command = new { EnvelopeId = envelopeId, FundAmount = fundAmount };
    
    using var response = await http.PutAsJsonAsync("envelopes/fundamount", command, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<UpdateFundAmountResponse>(cancellationToken: cancellationToken);
    
    if (result is null)
    {
      logger.LogDebug("Null response for UpdateFundAmountResponse from envelopes/fundamount");
      throw new InvalidOperationException("Expected non-null UpdateFundAmountResponse from 'envelopes/fundamount'.");
    }
    
    return result;
  }

  public async Task<ClearAllFundAmountsResponse> ClearAllFundAmountsAsync(CancellationToken cancellationToken = default)
  {
    using var response = await http.PostAsync("envelopes/clearallfundamounts", null, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<ClearAllFundAmountsResponse>(cancellationToken: cancellationToken);

    if (result is null)
    {
      logger.LogDebug("Null response for ClearAllFundAmountsResponse from envelopes/clearallfundamounts");
      throw new InvalidOperationException("Expected non-null ClearAllFundAmountsResponse from 'envelopes/clearallfundamounts'.");
    }

    return result;
  }
}

/// <summary>
/// Internal record for deserializing success response from fund endpoint
/// </summary>
file record FundSuccessResponse(int FundedCount);

/// <summary>
/// Internal record for deserializing error response from fund endpoint
/// </summary>
file record FundErrorResponse(string Error);
