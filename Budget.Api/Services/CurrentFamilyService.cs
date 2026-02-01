using System.Security.Claims;

namespace Budget.Api.Services;

/// <summary>
/// Service implementation for retrieving the current user's FamilyId from authentication context
/// </summary>
public class CurrentFamilyService(IHttpContextAccessor httpContextAccessor) : ICurrentFamilyService
{
  /// <summary>
  /// Gets the FamilyId of the currently authenticated user from claims
  /// </summary>
  /// <returns>The FamilyId from claims</returns>
  /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated or FamilyId claim is missing/invalid</exception>
  public int GetCurrentFamilyId()
  {
    var user = httpContextAccessor.HttpContext?.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
      throw new UnauthorizedAccessException("User is not authenticated.");
    }

    var familyIdClaim = user.FindFirst("FamilyId")?.Value;
    if (!int.TryParse(familyIdClaim, out var familyId))
    {
      throw new UnauthorizedAccessException("User authenticated but FamilyId claim is missing or invalid.");
    }

    return familyId;
  }
}