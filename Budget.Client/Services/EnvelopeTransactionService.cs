using Budget.Client.Components.Envelopes;

namespace Budget.Client.Services;

/// <summary>
/// Service for managing envelope transaction operations
/// </summary>
public class EnvelopeTransactionService(
  IBudgetApiClient api,
  IDialogService dialogService,
  IUserAndOptions userOptions,
  ILogger<EnvelopeTransactionService> logger) : IEnvelopeTransactionService
{
  /// <summary>
  /// Loads transactions for a specific envelope
  /// </summary>
  /// <param name="envelopeId">The envelope ID to load transactions for</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>List of transactions for the envelope</returns>
  public async Task<List<TransactionDto>> LoadTransactionsAsync(int envelopeId, CancellationToken cancellationToken = default)
  {
    try
    {
      var result = await api.GetTransactionsByEnvelopeAsync(envelopeId, cancellationToken);
      return [.. result];
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed loading transactions for envelope {EnvelopeId}", envelopeId);
      return [];
    }
  }

  /// <summary>
  /// Shows transaction details dialog - editable for admin users, read-only for others
  /// </summary>
  /// <param name="transactionId">The transaction ID to show</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result containing updated envelopes if transaction was edited, null if canceled or read-only</returns>
  public async Task<TransactionDialogResult?> ShowTransactionDetailsAsync(int transactionId, CancellationToken cancellationToken = default)
  {
    try
    {
      var detail = await api.GetOneTransactionDetailAsync(transactionId, cancellationToken);

      if (userOptions.IsAdminUser())
      {
        // Admin users can edit transactions via EditTransactionDialog
        var parameters = new DialogParameters { [nameof(EditTransactionDialog.ExistingTransaction)] = detail };
        var options = new DialogOptions
          { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        var dialog = await dialogService.ShowAsync<EditTransactionDialog>("Edit Transaction", parameters, options);
        var result = await dialog.Result;
        
        if (result is { Canceled: false, Data: List<EnvelopeDto> envResult })
        {
          return new TransactionDialogResult
          {
            WasEdited = true,
            UpdatedEnvelopes = envResult
          };
        }
      }
      else
      {
        // Non-admin users see read-only ShowOneTransaction dialog
        var parameters = new DialogParameters { [nameof(ShowOneTransaction.Transaction)] = detail };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        await dialogService.ShowAsync<ShowOneTransaction>("Transaction Details", parameters, options);
      }

      return null;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed loading transaction detail for transaction {TransactionId}", transactionId);
      return null;
    }
  }

  /// <summary>
  /// Shows the new transaction dialog for creating a purchase in the specified envelope
  /// </summary>
  /// <param name="envelopeId">The envelope ID to create the transaction in</param>
  /// <returns>Result containing updated envelopes if transaction was created, null if canceled</returns>
  public async Task<TransactionDialogResult?> ShowNewTransactionDialogAsync(int envelopeId)
  {
    try
    {
      var parameters = new DialogParameters { [nameof(EditTransactionDialog.InitialEnvelopeId)] = envelopeId };
      var options = new DialogOptions
        { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
      var dialog = await dialogService.ShowAsync<EditTransactionDialog>("New Purchase", parameters, options);
      var result = await dialog.Result;

      if (result is { Canceled: false, Data: List<EnvelopeDto> envResult })
      {
        return new TransactionDialogResult
        {
          WasEdited = true,
          UpdatedEnvelopes = envResult
        };
      }

      return null;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed creating new transaction for envelope {EnvelopeId}", envelopeId);
      return null;
    }
  }
}

/// <summary>
/// Interface for envelope transaction operations
/// </summary>
public interface IEnvelopeTransactionService
{
  /// <summary>
  /// Loads transactions for a specific envelope
  /// </summary>
  Task<List<TransactionDto>> LoadTransactionsAsync(int envelopeId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Shows transaction details dialog - editable for admin users, read-only for others
  /// </summary>
  Task<TransactionDialogResult?> ShowTransactionDetailsAsync(int transactionId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Shows the new transaction dialog for creating a purchase in the specified envelope
  /// </summary>
  Task<TransactionDialogResult?> ShowNewTransactionDialogAsync(int envelopeId);
}

/// <summary>
/// Result of a transaction dialog operation
/// </summary>
public class TransactionDialogResult
{
  public bool WasEdited { get; set; }
  public List<EnvelopeDto> UpdatedEnvelopes { get; set; } = [];
}
