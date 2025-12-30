namespace Budget.DB;

/// <summary>
/// Service for retrieving the current user's FamilyId from authentication context
/// </summary>
public interface ICurrentFamilyService
{
  /// <summary>
  /// Gets the FamilyId of the currently authenticated user
  /// </summary>
  /// <returns>The FamilyId, or 1 as default if not authenticated</returns>
  int GetCurrentFamilyId();
}
