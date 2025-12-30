using Budget.DB;
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
  /// <returns>The FamilyId from claims, or 1 as default if not found or not authenticated</returns>
  public int GetCurrentFamilyId()
  {
    var user = httpContextAccessor.HttpContext?.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
      return 1; // Default family for unauthenticated requests
    }

    var familyIdClaim = user.FindFirst("FamilyId")?.Value;
    if (int.TryParse(familyIdClaim, out var familyId))
    {
      return familyId;
    }

    return 1; // Default family if claim not found
  }
}
