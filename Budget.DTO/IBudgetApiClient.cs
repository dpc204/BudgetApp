namespace Budget.DTO;

public interface IBudgetApiClient
{
    Task<IReadOnlyList<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);
}

public sealed record EnvelopeDto(int Id, string Name, decimal Balance, decimal Budget, int CategoryId, int SortOrder);
public sealed record CategoryDto(int Id, string Name, string Description, int SortOrder);
public sealed record TransactionDto(int TransactionId, int LineId, string Description, decimal Amount, DateTime Date);

