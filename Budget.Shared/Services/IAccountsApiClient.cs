namespace Budget.Shared.Services;

/// <summary>
/// API client for bank account-related operations
/// </summary>
public interface IAccountsApiClient
{
  Task<List<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
  Task<BankAccountDto> AddAccountAsync(BankAccountDto dto, CancellationToken cancellationToken = default);
  Task<BankAccountDto> UpdateAccountAsync(BankAccountDto dto, CancellationToken cancellationToken = default);
  Task<bool> RemoveAccountAsync(int id, CancellationToken cancellationToken = default);
}
