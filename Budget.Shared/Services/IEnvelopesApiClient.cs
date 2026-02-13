namespace Budget.Shared.Services;

/// <summary>
/// API client for envelope-related operations
/// </summary>
public interface IEnvelopesApiClient
{
  // Read operations (runtime)
  Task<List<EnvelopeDto>> GetEnvelopesAsync(EnvelopeTypes envelopeType = EnvelopeTypes.All, CancellationToken cancellationToken = default);
  Task<EnvelopeDto> GetEnvelopeByIdAsync(int envelopeId, CancellationToken cancellationToken = default);
  Task<EnvelopeDto> GetEnvelopeByEnvelopeTypeAsync(EnvelopeTypes envType, CancellationToken cancellationToken = default);

  // Fund operations (budget planning)
  Task<FBResult<int>> FundEnvelopesAsync(CancellationToken cancellationToken);
  Task<UpdateFundAmountResponse> UpdateFundAmountAsync(int envelopeId, decimal? fundAmount, CancellationToken cancellationToken = default);
  Task<ClearAllFundAmountsResponse> ClearAllFundAmountsAsync(CancellationToken cancellationToken = default);

  // Maintenance operations (admin)
  Task<IEnumerable<EnvelopeDto>> GetEnvelopesDtoAsync(CancellationToken cancellationToken = default);
  Task<EnvelopeDto> AddAsync(EnvelopeDto dto);
  Task<EnvelopeUpdateDto> UpdateAsync(EnvelopeUpdateDto dto, CancellationToken cancellationToken = default);
  Task<bool> RemoveEnvelopeAsync(int id, CancellationToken cancellationToken = default);
  Task<ImportResult> ImportEnvelopesAsync(string csvContent, CancellationToken cancellationToken = default);
  Task<string> ExportEnvelopesAsync(CancellationToken cancellationToken = default);
  Task<int> GetEnvelopeTransactionCountAsync(int envelopeId, CancellationToken cancellationToken = default);
}
