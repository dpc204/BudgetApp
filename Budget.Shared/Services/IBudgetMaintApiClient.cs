namespace Budget.Shared.Services;

public interface IBudgetMaintApiClient
{
  Task<IEnumerable<EnvelopeDto>> GetEnvelopesDtoAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);
  Task<OneTransactionDetail> GetOneTransactionDetailAsync(int transactionId, CancellationToken cancellationToken = default);
  Task<EnvelopeDto> AddAsync(EnvelopeDto dto);
  Task<EnvelopeUpdateDto> UpdateAsync(EnvelopeUpdateDto dto, CancellationToken cancellationToken = default); // new for editing
  Task<bool> RemoveEnvelopeAsync(int id, CancellationToken cancellationToken = default);
  Task<ImportResult> ImportEnvelopesAsync(string csvContent, CancellationToken cancellationToken = default);
  Task<string> ExportEnvelopesAsync(CancellationToken cancellationToken = default);
  Task<int> GetEnvelopeTransactionCountAsync(int envelopeId, CancellationToken cancellationToken = default);

  // Category maintenance
  Task<CategoryDto> AddCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default);
  Task<CategoryDto> UpdateCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default);
  Task<bool> RemoveCategoryAsync(string id, CancellationToken cancellationToken = default);
  Task<ImportResult> ImportCategoriesAsync(string csvContent, CancellationToken cancellationToken = default);
  Task<string> ExportCategoriesAsync(CancellationToken cancellationToken = default);

  // Account maintenance
  Task<IEnumerable<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
  Task<BankAccountDto> AddAccountAsync(BankAccountDto dto, CancellationToken cancellationToken = default);
  Task<BankAccountDto> UpdateAccountAsync(BankAccountDto dto, CancellationToken cancellationToken = default);
  Task<bool> RemoveAccountAsync(int id, CancellationToken cancellationToken = default);

  // Backup maintenance
  Task<BackupPlanDto> GetBackupPlanAsync(CancellationToken cancellationToken = default);
  Task<ExportAllResponse> ExportAllTablesAsync(CancellationToken cancellationToken = default);
  Task<BackupStatusDto?> GetBackupStatusAsync(string backupId, CancellationToken cancellationToken = default);
  Task<IEnumerable<BackupSetDto>> GetBackupSetsAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<BackupTableDto>> GetBackupSetDetailsAsync(string partitionKey, CancellationToken cancellationToken = default);
  Task<bool> DeleteBackupSetAsync(string partitionKey, CancellationToken cancellationToken = default);
  Task<FileDownloadDto> DownloadBackupCsvAsync(string blobName, CancellationToken cancellationToken = default);
  Task<FileDownloadDto> DownloadDatabaseBackupAsync(string fileName, CancellationToken cancellationToken = default);

  // Role management
  Task<IEnumerable<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
  Task<RoleDto?> GetRoleAsync(int id, CancellationToken cancellationToken = default);
  Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
  Task<RoleDto> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
  Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);

  // User management
  Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
  Task<UserDetailDto?> GetUserAsync(int id, CancellationToken cancellationToken = default);
  Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);

  // User-Role management
  Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
  Task<AssignRoleResponse> AssignRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default);
  Task<bool> RemoveRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default);
}

public sealed record BackupPlanDto(string FileName);
public sealed record ImportResult(int ImportedCount, List<string> Errors);
public sealed record ExportAllResponse(string BackupId, string Message);
public sealed record BackupStatusDto(
  string BackupId,
  DateTime StartTime,
  DateTime? EndTime,
  int TotalTables,
  int CompletedTables,
  int FailedTables,
  string? CurrentTable,
  string? ErrorMessage,
  bool IsComplete);

public sealed record FileDownloadDto(byte[] Content, string FileName, string ContentType);

// Role management DTOs
public sealed record RoleDto(int Id, string Name, string Description, DateTime CreatedAt, DateTime? ModifiedAt, int UserCount);
public sealed record CreateRoleRequest(string Name, string Description);
public sealed record UpdateRoleRequest(int Id, string Name, string Description);

// User management DTOs
public sealed record UserDto(int Id, string Email, string FirstName, string LastName, int FamilyId, List<string> Roles);
public sealed record UserDetailDto(int Id, string Email, string FirstName, string LastName, int FamilyId, List<RoleInfoDto> Roles);
public sealed record RoleInfoDto(int Id, string Name);
public sealed record UpdateUserRequest(int Id, string Email, string FirstName, string LastName, int FamilyId);

// User-Role management DTOs
public sealed record UserRoleDto(int RoleId, string RoleName, DateTime AssignedAt, int? AssignedByUserId, string? AssignedByName);
public sealed record AssignRoleResponse(int UserId, int RoleId, string RoleName, DateTime AssignedAt);

