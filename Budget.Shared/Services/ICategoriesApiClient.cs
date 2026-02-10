namespace Budget.Shared.Services;

/// <summary>
/// API client for category-related operations
/// </summary>
public interface ICategoriesApiClient
{
  // Read operations (runtime)
  Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

  // Maintenance operations (admin)
  Task<CategoryDto> AddCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default);
  Task<CategoryDto> UpdateCategoryAsync(CategoryDto dto, CancellationToken cancellationToken = default);
  Task<bool> RemoveCategoryAsync(string id, CancellationToken cancellationToken = default);
  Task<ImportResult> ImportCategoriesAsync(string csvContent, CancellationToken cancellationToken = default);
  Task<string> ExportCategoriesAsync(CancellationToken cancellationToken = default);
}
