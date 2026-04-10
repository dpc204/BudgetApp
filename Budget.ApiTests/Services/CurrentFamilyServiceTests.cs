using Budget.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Security.Claims;
using System.Security.Principal;

namespace Budget.ApiTests.Services;

/// <summary>
/// Unit tests for CurrentFamilyService
/// </summary>
public class CurrentFamilyServiceTests
{
  /// <summary>
  /// Tests that GetCurrentFamilyId throws UnauthorizedAccessException when HttpContext is null.
  /// Input: HttpContext is null
  /// Expected: UnauthorizedAccessException with message "HttpContext is not available."
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_HttpContextIsNull_ThrowsUnauthorizedAccessException()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    mockAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act & Assert
    var exception = Assert.Throws<UnauthorizedAccessException>(() => service.GetCurrentFamilyId());
    Assert.Equal("HttpContext is not available.", exception.Message);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId throws UnauthorizedAccessException when User is null.
  /// Input: HttpContext.User is null
  /// Expected: UnauthorizedAccessException with message "User is not authenticated."
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_UserIsNull_ThrowsUnauthorizedAccessException()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    mockHttpContext.Setup(x => x.User).Returns((ClaimsPrincipal)null!);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act & Assert
    var exception = Assert.Throws<UnauthorizedAccessException>(() => service.GetCurrentFamilyId());
    Assert.Equal("User is not authenticated.", exception.Message);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId throws UnauthorizedAccessException when User.Identity is null.
  /// Input: HttpContext.User.Identity is null
  /// Expected: UnauthorizedAccessException with message "User is not authenticated."
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_UserIdentityIsNull_ThrowsUnauthorizedAccessException()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var user = new ClaimsPrincipal();
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act & Assert
    var exception = Assert.Throws<UnauthorizedAccessException>(() => service.GetCurrentFamilyId());
    Assert.Equal("User is not authenticated.", exception.Message);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId throws UnauthorizedAccessException when user is not authenticated.
  /// Input: User.Identity.IsAuthenticated is false
  /// Expected: UnauthorizedAccessException with message "User is not authenticated."
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_UserNotAuthenticated_ThrowsUnauthorizedAccessException()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(false);
    var user = new ClaimsPrincipal(mockIdentity.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act & Assert
    var exception = Assert.Throws<UnauthorizedAccessException>(() => service.GetCurrentFamilyId());
    Assert.Equal("User is not authenticated.", exception.Message);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns FamilyId from header when valid header exists.
  /// Input: Authenticated user, valid "X-FamilyId" header with value "123"
  /// Expected: Returns 123
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_ValidHeaderExists_ReturnsFamilyIdFromHeader()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var identity = new ClaimsIdentity("TestAuthType");
    var user = new ClaimsPrincipal(identity);

    var headerValue = new StringValues("123");
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(123, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns zero when header contains "0".
  /// Input: Authenticated user, "X-FamilyId" header with value "0"
  /// Expected: Returns 0
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_HeaderWithZero_ReturnsZero()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var identity = new ClaimsIdentity("TestAuthType");
    var user = new ClaimsPrincipal(identity);

    var headerValue = new StringValues("0");
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(0, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns int.MaxValue when header contains int.MaxValue.
  /// Input: Authenticated user, "X-FamilyId" header with value "2147483647"
  /// Expected: Returns int.MaxValue (2147483647)
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_HeaderWithIntMaxValue_ReturnsIntMaxValue()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var identity = new ClaimsIdentity("TestAuthenticationType");
    var user = new ClaimsPrincipal(identity);

    var headerValue = new StringValues(int.MaxValue.ToString());
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(int.MaxValue, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns int.MinValue when header contains int.MinValue.
  /// Input: Authenticated user, "X-FamilyId" header with value "-2147483648"
  /// Expected: Returns int.MinValue (-2147483648)
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_HeaderWithIntMinValue_ReturnsIntMinValue()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var identity = new ClaimsIdentity("TestAuthType");
    var user = new ClaimsPrincipal(identity);

    var headerValue = new StringValues(int.MinValue.ToString());
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(int.MinValue, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId falls back to claim when header exists but is invalid.
  /// Input: Authenticated user, "X-FamilyId" header with value "invalid", valid claim "456"
  /// Expected: Returns 456 from claim
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_InvalidHeaderValidClaim_ReturnsFamilyIdFromClaim()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "456") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = new StringValues("invalid");
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(456, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId falls back to claim when header is empty string.
  /// Input: Authenticated user, "X-FamilyId" header with empty string, valid claim "789"
  /// Expected: Returns 789 from claim
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_EmptyHeaderValidClaim_ReturnsFamilyIdFromClaim()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "789") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = new StringValues(string.Empty);
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(789, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId falls back to claim when header is whitespace only.
  /// Input: Authenticated user, "X-FamilyId" header with whitespace, valid claim "111"
  /// Expected: Returns 111 from claim
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_WhitespaceHeaderValidClaim_ReturnsFamilyIdFromClaim()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "111") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = new StringValues("   ");
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(111, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns FamilyId from claim when header doesn't exist.
  /// Input: Authenticated user, no "X-FamilyId" header, valid claim "222"
  /// Expected: Returns 222 from claim
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_NoHeaderValidClaim_ReturnsFamilyIdFromClaim()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "222") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = StringValues.Empty;
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(false);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(222, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns zero when claim contains "0".
  /// Input: Authenticated user, no header, claim with value "0"
  /// Expected: Returns 0
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_NoHeaderClaimWithZero_ReturnsZero()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "0") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = StringValues.Empty;
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(false);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(0, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns int.MaxValue when claim contains int.MaxValue.
  /// Input: Authenticated user, no header, claim with value "2147483647"
  /// Expected: Returns int.MaxValue (2147483647)
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_NoHeaderClaimWithIntMaxValue_ReturnsIntMaxValue()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", int.MaxValue.ToString()) };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = StringValues.Empty;
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(false);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(int.MaxValue, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns int.MinValue when claim contains int.MinValue.
  /// Input: Authenticated user, no header, claim with value "-2147483648"
  /// Expected: Returns int.MinValue (-2147483648)
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_NoHeaderClaimWithIntMinValue_ReturnsIntMinValue()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", int.MinValue.ToString()) };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = StringValues.Empty;
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(false);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(int.MinValue, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId throws UnauthorizedAccessException when no header and no claim exists.
  /// Input: Authenticated user, no "X-FamilyId" header, no "FamilyId" claim
  /// Expected: UnauthorizedAccessException with message "User authenticated but FamilyId is missing from both header and claims."
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_NoHeaderNoClaim_ThrowsUnauthorizedAccessException()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var user = new ClaimsPrincipal(new ClaimsIdentity([], "TestAuth"));

    var headerValue = StringValues.Empty;
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(false);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act & Assert
    var exception = Assert.Throws<UnauthorizedAccessException>(() => service.GetCurrentFamilyId());
    Assert.Equal("User authenticated but FamilyId is missing from both header and claims.", exception.Message);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId throws UnauthorizedAccessException when no header and claim is invalid.
  /// Input: Authenticated user, no header, claim with invalid value "not-a-number"
  /// Expected: UnauthorizedAccessException with message "User authenticated but FamilyId is missing from both header and claims."
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_NoHeaderInvalidClaim_ThrowsUnauthorizedAccessException()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "not-a-number") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = StringValues.Empty;
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(false);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act & Assert
    var exception = Assert.Throws<UnauthorizedAccessException>(() => service.GetCurrentFamilyId());
    Assert.Equal("User authenticated but FamilyId is missing from both header and claims.", exception.Message);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId throws UnauthorizedAccessException when no header and claim is empty string.
  /// Input: Authenticated user, no header, claim with empty string value
  /// Expected: UnauthorizedAccessException with message "User authenticated but FamilyId is missing from both header and claims."
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_NoHeaderEmptyClaim_ThrowsUnauthorizedAccessException()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", string.Empty) };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = StringValues.Empty;
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(false);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act & Assert
    var exception = Assert.Throws<UnauthorizedAccessException>(() => service.GetCurrentFamilyId());
    Assert.Equal("User authenticated but FamilyId is missing from both header and claims.", exception.Message);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId throws UnauthorizedAccessException when no header and claim is whitespace only.
  /// Input: Authenticated user, no header, claim with whitespace only
  /// Expected: UnauthorizedAccessException with message "User authenticated but FamilyId is missing from both header and claims."
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_NoHeaderWhitespaceClaim_ThrowsUnauthorizedAccessException()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "   ") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = StringValues.Empty;
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(false);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act & Assert
    var exception = Assert.Throws<UnauthorizedAccessException>(() => service.GetCurrentFamilyId());
    Assert.Equal("User authenticated but FamilyId is missing from both header and claims.", exception.Message);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns header value when both header and claim exist with different values.
  /// Input: Authenticated user, "X-FamilyId" header with value "100", claim with value "200"
  /// Expected: Returns 100 (header takes precedence)
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_BothHeaderAndClaimExist_ReturnsHeaderValue()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "200") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = new StringValues("100");
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(100, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId falls back to claim when header contains decimal value.
  /// Input: Authenticated user, "X-FamilyId" header with value "123.45", valid claim "333"
  /// Expected: Returns 333 from claim
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_HeaderWithDecimalValidClaim_ReturnsFamilyIdFromClaim()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "333") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = new StringValues("123.45");
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(333, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId falls back to claim when header contains value exceeding int.MaxValue.
  /// Input: Authenticated user, "X-FamilyId" header with value exceeding int.MaxValue, valid claim "444"
  /// Expected: Returns 444 from claim
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_HeaderWithValueExceedingIntMaxValidClaim_ReturnsFamilyIdFromClaim()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "444") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = new StringValues("9999999999999");
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(444, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns negative value when header contains negative value.
  /// Input: Authenticated user, "X-FamilyId" header with value "-5"
  /// Expected: Returns -5
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_HeaderWithNegativeValue_ReturnsNegativeValue()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var identity = new ClaimsIdentity(authenticationType: "TestAuthType");
    var user = new ClaimsPrincipal(identity);

    var headerValue = new StringValues("-5");
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(true);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(-5, result);
  }

  /// <summary>
  /// Tests that GetCurrentFamilyId returns negative value when claim contains negative value.
  /// Input: Authenticated user, no header, claim with value "-10"
  /// Expected: Returns -10
  /// </summary>
  [Fact]
  public void GetCurrentFamilyId_NoHeaderClaimWithNegativeValue_ReturnsNegativeValue()
  {
    // Arrange
    var mockAccessor = new Mock<IHttpContextAccessor>();
    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockHeaders = new Mock<IHeaderDictionary>();
    var mockIdentity = new Mock<IIdentity>();
    mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
    var claims = new List<Claim> { new("FamilyId", "-10") };
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var headerValue = StringValues.Empty;
    mockHeaders.Setup(x => x.TryGetValue("X-FamilyId", out headerValue)).Returns(false);
    mockRequest.Setup(x => x.Headers).Returns(mockHeaders.Object);
    mockHttpContext.Setup(x => x.User).Returns(user);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
    var service = new CurrentFamilyService(mockAccessor.Object);

    // Act
    var result = service.GetCurrentFamilyId();

    // Assert
    Assert.Equal(-10, result);
  }
}