using System.Security.Claims;

namespace Budget.Api.Services;

/// <summary>
/// Service implementation for retrieving the current user's FamilyId from authentication context
/// </summary>
public class CurrentFamilyService(IHttpContextAccessor httpContextAccessor) : ICurrentFamilyService
{
  /// <summary>
  /// Gets the FamilyId of the currently authenticated user from custom header or claims
  /// </summary>
  /// <returns>The FamilyId from header or claims</returns>
  /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated or FamilyId is missing/invalid</exception>
  public int GetCurrentFamilyId()
  {
    var httpContext = httpContextAccessor.HttpContext;
    if (httpContext == null)
    {
      throw new UnauthorizedAccessException("HttpContext is not available.");
    }

    var user = httpContext.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
      throw new UnauthorizedAccessException("User is not authenticated.");
    }

    // Try to get FamilyId from custom header first (sent by Budget.Web)
    if (httpContext.Request.Headers.TryGetValue("X-FamilyId", out var headerValue))
    {
      if (int.TryParse(headerValue.ToString(), out var familyIdFromHeader))
      {
        return familyIdFromHeader;
      }
    }

    // Fall back to claim (for local JWT tokens or future implementations)
    var familyIdClaim = user.FindFirst("FamilyId")?.Value;
    if (!int.TryParse(familyIdClaim, out var familyId))
    {
      throw new UnauthorizedAccessException("User authenticated but FamilyId is missing from both header and claims.");
    }

    return familyId;
  }
}