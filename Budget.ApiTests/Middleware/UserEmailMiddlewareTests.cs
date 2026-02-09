using System.Security.Claims;
using System.Security.Principal;
using Budget.Api.Middleware;
using Budget.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Budget.ApiTests.Middleware;

/// <summary>
/// Unit tests for the <see cref="UserEmailMiddleware"/> class.
/// </summary>
public sealed class UserEmailMiddlewareTests
{
    /// <summary>
    /// Tests that InvokeAsync skips processing and calls next delegate when user is not authenticated (Identity is null).
    /// Input: HttpContext with User.Identity = null
    /// Expected: next delegate is called, no email processing occurs
    /// </summary>
    [Fact]
    public async Task InvokeAsync_UnauthenticatedUserWithNullIdentity_SkipsProcessingAndCallsNext()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockHttpContext = new Mock<HttpContext>();
        var mockUser = new Mock<ClaimsPrincipal>();
        mockUser.Setup(u => u.Identity).Returns((IIdentity?)null);
        mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync skips processing and calls next delegate when user Identity.IsAuthenticated is false.
    /// Input: HttpContext with User.Identity.IsAuthenticated = false
    /// Expected: next delegate is called, no email processing occurs
    /// </summary>
    [Fact]
    public async Task InvokeAsync_UnauthenticatedUserWithIsAuthenticatedFalse_SkipsProcessingAndCallsNext()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockHttpContext = new Mock<HttpContext>();
        var mockIdentity = new Mock<IIdentity>();
        mockIdentity.Setup(i => i.IsAuthenticated).Returns(false);
        var mockUser = new Mock<ClaimsPrincipal>();
        mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);
        mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync skips processing for various path patterns that should be excluded.
    /// Input: Authenticated user with paths matching exclusion patterns
    /// Expected: next delegate is called, no email processing occurs for all excluded paths
    /// </summary>
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/check")]
    [InlineData("/openapi")]
    [InlineData("/openapi/v1")]
    [InlineData("/scalar")]
    [InlineData("/scalar/docs")]
    [InlineData("/api/file.json")]
    [InlineData("/static/image.png")]
    [InlineData("/assets/style.css")]
    public async Task InvokeAsync_ExcludedPaths_SkipsProcessingAndCallsNext(string path)
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockHttpContext = new Mock<HttpContext>();
        var mockIdentity = new Mock<IIdentity>();
        mockIdentity.Setup(i => i.IsAuthenticated).Returns(true);
        var mockUser = new Mock<ClaimsPrincipal>();
        mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);
        mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString(path));

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync extracts email from ClaimTypes.Email claim when present.
    /// Input: Authenticated user with email in ClaimTypes.Email claim
    /// Expected: SetUserEmail is called with the email value
    /// </summary>
    [Fact]
    public async Task InvokeAsync_EmailInClaimTypesEmail_SetsUserEmail()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(new HeaderDictionary());

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("test@example.com"), Times.Once);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync extracts email from preferred_username claim when ClaimTypes.Email is not present.
    /// Input: Authenticated user with email in preferred_username claim
    /// Expected: SetUserEmail is called with the email from preferred_username
    /// </summary>
    [Fact]
    public async Task InvokeAsync_EmailInPreferredUsername_SetsUserEmail()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new("preferred_username", "preferred@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(new HeaderDictionary());

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("preferred@example.com"), Times.Once);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync extracts email from upn claim when ClaimTypes.Email and preferred_username are not present.
    /// Input: Authenticated user with email in upn claim
    /// Expected: SetUserEmail is called with the email from upn
    /// </summary>
    [Fact]
    public async Task InvokeAsync_EmailInUpn_SetsUserEmail()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new("upn", "upn@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(new HeaderDictionary());

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("upn@example.com"), Times.Once);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync prioritizes ClaimTypes.Email over other email claims when multiple are present.
    /// Input: Authenticated user with email in all three claim types
    /// Expected: SetUserEmail is called with the email from ClaimTypes.Email
    /// </summary>
    [Fact]
    public async Task InvokeAsync_MultipleEmailClaims_PrioritizesClaimTypesEmail()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "primary@example.com"),
            new("preferred_username", "preferred@example.com"),
            new("upn", "upn@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(new HeaderDictionary());

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("primary@example.com"), Times.Once);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync does not call SetUserEmail when no email claims are present.
    /// Input: Authenticated user without any email claims
    /// Expected: SetUserEmail is not called, but next delegate is still called
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NoEmailClaims_DoesNotSetUserEmail()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new("name", "Test User") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(new HeaderDictionary());

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail(It.IsAny<string>()), Times.Never);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync does not call SetUserEmail when email claim value is empty string.
    /// Input: Authenticated user with empty string email claim
    /// Expected: SetUserEmail is not called
    /// </summary>
    [Fact]
    public async Task InvokeAsync_EmptyStringEmail_DoesNotSetUserEmail()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, string.Empty) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(new HeaderDictionary());

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail(It.IsAny<string>()), Times.Never);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync does not throw when IUserAndOptions service is not available.
    /// Input: Authenticated user with email, but IUserAndOptions service returns null
    /// Expected: Warning is logged, next delegate is called, no exception thrown
    /// </summary>
    [Fact]
    public async Task InvokeAsync_UserAndOptionsServiceNotAvailable_LogsWarningAndCallsNext()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns((IUserAndOptions?)null);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync sets UserId and FamilyId when both headers are present and valid.
    /// Input: Valid X-UserId and X-FamilyId headers with integer values
    /// Expected: SetUserIdAndFamilyId is called with parsed values
    /// </summary>
    [Theory]
    [InlineData("123", "456", 123, 456)]
    [InlineData("0", "0", 0, 0)]
    [InlineData("1", "1", 1, 1)]
    [InlineData("2147483647", "2147483647", int.MaxValue, int.MaxValue)]
    public async Task InvokeAsync_ValidUserIdAndFamilyIdHeaders_SetsUserIdAndFamilyId(
        string userIdHeader, string familyIdHeader, int expectedUserId, int expectedFamilyId)
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var headers = new HeaderDictionary
        {
            { "X-UserId", new StringValues(userIdHeader) },
            { "X-FamilyId", new StringValues(familyIdHeader) }
        };

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(headers);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("test@example.com"), Times.Once);
        mockUserAndOptions.Verify(u => u.SetUserIdAndFamilyId(expectedUserId, expectedFamilyId), Times.Once);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync handles negative values in UserId and FamilyId headers.
    /// Input: Negative integer values in headers
    /// Expected: SetUserIdAndFamilyId is called with negative values
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NegativeUserIdAndFamilyIdHeaders_SetsNegativeValues()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var headers = new HeaderDictionary
        {
            { "X-UserId", new StringValues("-100") },
            { "X-FamilyId", new StringValues("-200") }
        };

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(headers);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserIdAndFamilyId(-100, -200), Times.Once);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync does not call SetUserIdAndFamilyId when UserId header fails to parse.
    /// Input: Invalid UserId header value
    /// Expected: SetUserIdAndFamilyId is not called, warning is logged
    /// </summary>
    [Theory]
    [InlineData("abc", "123")]
    [InlineData("", "123")]
    [InlineData("12.34", "123")]
    [InlineData("null", "123")]
    [InlineData("999999999999999", "123")]
    public async Task InvokeAsync_InvalidUserIdHeader_DoesNotSetUserIdAndFamilyId(string userIdHeader, string familyIdHeader)
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var headers = new HeaderDictionary
        {
            { "X-UserId", new StringValues(userIdHeader) },
            { "X-FamilyId", new StringValues(familyIdHeader) }
        };

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(headers);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("test@example.com"), Times.Once);
        mockUserAndOptions.Verify(u => u.SetUserIdAndFamilyId(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync does not call SetUserIdAndFamilyId when FamilyId header fails to parse.
    /// Input: Invalid FamilyId header value
    /// Expected: SetUserIdAndFamilyId is not called, warning is logged
    /// </summary>
    [Theory]
    [InlineData("123", "xyz")]
    [InlineData("123", "")]
    [InlineData("123", "45.67")]
    [InlineData("123", "999999999999999")]
    public async Task InvokeAsync_InvalidFamilyIdHeader_DoesNotSetUserIdAndFamilyId(string userIdHeader, string familyIdHeader)
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var headers = new HeaderDictionary
        {
            { "X-UserId", new StringValues(userIdHeader) },
            { "X-FamilyId", new StringValues(familyIdHeader) }
        };

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(headers);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("test@example.com"), Times.Once);
        mockUserAndOptions.Verify(u => u.SetUserIdAndFamilyId(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync does not call SetUserIdAndFamilyId when only X-UserId header is present.
    /// Input: Only X-UserId header present
    /// Expected: SetUserIdAndFamilyId is not called
    /// </summary>
    [Fact]
    public async Task InvokeAsync_OnlyUserIdHeader_DoesNotSetUserIdAndFamilyId()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var headers = new HeaderDictionary
        {
            { "X-UserId", new StringValues("123") }
        };

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(headers);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("test@example.com"), Times.Once);
        mockUserAndOptions.Verify(u => u.SetUserIdAndFamilyId(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync does not call SetUserIdAndFamilyId when only X-FamilyId header is present.
    /// Input: Only X-FamilyId header present
    /// Expected: SetUserIdAndFamilyId is not called
    /// </summary>
    [Fact]
    public async Task InvokeAsync_OnlyFamilyIdHeader_DoesNotSetUserIdAndFamilyId()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var headers = new HeaderDictionary
        {
            { "X-FamilyId", new StringValues("456") }
        };

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(headers);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("test@example.com"), Times.Once);
        mockUserAndOptions.Verify(u => u.SetUserIdAndFamilyId(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync does not call SetUserIdAndFamilyId when neither header is present.
    /// Input: No X-UserId or X-FamilyId headers
    /// Expected: SetUserIdAndFamilyId is not called
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NoHeaders_DoesNotSetUserIdAndFamilyId()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var headers = new HeaderDictionary();

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(headers);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("test@example.com"), Times.Once);
        mockUserAndOptions.Verify(u => u.SetUserIdAndFamilyId(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync always calls next delegate regardless of processing outcome.
    /// Input: Various scenarios including authenticated users, unauthenticated users, and excluded paths
    /// Expected: next delegate is always called exactly once
    /// </summary>
    [Theory]
    [InlineData(true, "/api/test")]
    [InlineData(false, "/api/test")]
    [InlineData(true, "/health")]
    [InlineData(true, "/api/file.json")]
    public async Task InvokeAsync_VariousScenarios_AlwaysCallsNext(bool isAuthenticated, string path)
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockHttpContext = new Mock<HttpContext>();
        var mockIdentity = new Mock<IIdentity>();
        mockIdentity.Setup(i => i.IsAuthenticated).Returns(isAuthenticated);
        var mockUser = new Mock<ClaimsPrincipal>();
        mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);
        mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString(path));

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync handles whitespace-only strings in headers.
    /// Input: X-UserId and X-FamilyId headers with whitespace values
    /// Expected: SetUserIdAndFamilyId is not called
    /// </summary>
    [Theory]
    [InlineData(" ", "123")]
    [InlineData("123", " ")]
    [InlineData("   ", "   ")]
    public async Task InvokeAsync_WhitespaceHeaders_DoesNotSetUserIdAndFamilyId(string userIdHeader, string familyIdHeader)
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var headers = new HeaderDictionary
        {
            { "X-UserId", new StringValues(userIdHeader) },
            { "X-FamilyId", new StringValues(familyIdHeader) }
        };

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(headers);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail("test@example.com"), Times.Once);
        mockUserAndOptions.Verify(u => u.SetUserIdAndFamilyId(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync handles int.MinValue in headers correctly.
    /// Input: X-UserId and X-FamilyId headers with int.MinValue
    /// Expected: SetUserIdAndFamilyId is called with int.MinValue
    /// </summary>
    [Fact]
    public async Task InvokeAsync_IntMinValueHeaders_SetsMinValues()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var headers = new HeaderDictionary
        {
            { "X-UserId", new StringValues(int.MinValue.ToString()) },
            { "X-FamilyId", new StringValues(int.MinValue.ToString()) }
        };

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(headers);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserIdAndFamilyId(int.MinValue, int.MinValue), Times.Once);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that InvokeAsync handles email edge cases with special characters.
    /// Input: Email with special but valid characters
    /// Expected: SetUserEmail is called with the email containing special characters
    /// </summary>
    [Theory]
    [InlineData("test+tag@example.com")]
    [InlineData("user.name@sub.example.com")]
    [InlineData("test_user@example.co.uk")]
    [InlineData("123@example.com")]
    public async Task InvokeAsync_EmailWithSpecialCharacters_SetsUserEmail(string email)
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<UserEmailMiddleware>>();
        var middleware = new UserEmailMiddleware(mockNext.Object, mockLogger.Object);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IUserAndOptions)))
            .Returns(mockUserAndOptions.Object);

        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(user);
        mockHttpContext.Setup(c => c.Request.Path).Returns(new PathString("/api/test"));
        mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
        mockHttpContext.Setup(c => c.Request.Headers).Returns(new HeaderDictionary());

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        mockUserAndOptions.Verify(u => u.SetUserEmail(email), Times.Once);
        mockNext.Verify(n => n(mockHttpContext.Object), Times.Once);
    }
}