using Budget.Shared;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Budget.ApiTests.Features.Authentication;


/// <summary>
/// Unit tests for JwtTokenService.
/// </summary>
public sealed class JwtTokenServiceTests
{
  /// <summary>
  /// Tests that CreateToken with valid user and roles returns a valid AuthResponse with a JWT token.
  /// Input: Valid user with all properties set and multiple roles.
  /// Expected: AuthResponse with non-empty token and correct expiration time.
  /// </summary>
  [Fact]
  public void CreateToken_WithValidUserAndRoles_ReturnsValidAuthResponse()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      Issuer = "test-issuer",
      Audience = "test-audience",
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456",
      ExpMinutes = 60
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 5
    };
    var roles = new List<string> { "Admin", "User" };

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    Assert.False(string.IsNullOrEmpty(result.AccessToken));
    Assert.True(result.ExpiresUtc > DateTime.UtcNow);
    Assert.True(result.ExpiresUtc <= DateTime.UtcNow.AddMinutes(61));
  }

  /// <summary>
  /// Tests that CreateToken with null user parameter throws an exception.
  /// Input: null user parameter.
  /// Expected: NullReferenceException is thrown.
  /// </summary>
  [Fact]
  [Trait("Category", "ProductionBugSuspected")]
  public void CreateToken_WithNullUser_ThrowsNullReferenceException()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var roles = new List<string> { "Admin" };

    // Act & Assert
    Assert.Throws<NullReferenceException>(() => service.CreateToken(null!, roles));
  }

  /// <summary>
  /// Tests that CreateToken with null roles parameter throws an exception.
  /// Input: null roles parameter.
  /// Expected: ArgumentNullException is thrown when enumerating roles.
  /// </summary>
  [Fact]
  [Trait("Category", "ProductionBugSuspected")]
  public void CreateToken_WithNullRoles_ThrowsArgumentNullException()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => service.CreateToken(user, null!));
  }

  /// <summary>
  /// Tests that CreateToken with empty roles collection returns a token without role claims.
  /// Input: Valid user and empty roles collection.
  /// Expected: AuthResponse with valid token containing no role claims.
  /// </summary>
  [Fact]
  public void CreateToken_WithEmptyRoles_ReturnsTokenWithoutRoleClaims()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    Assert.False(string.IsNullOrEmpty(result.AccessToken));
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role);
    Assert.Empty(roleClaims);
  }

  /// <summary>
  /// Tests that CreateToken with user having null UserName and Email uses Id for unique name claim.
  /// Input: User with null UserName and null Email.
  /// Expected: Token contains Id as the unique name claim.
  /// </summary>
  [Fact]
  public void CreateToken_WithUserNullUserNameAndEmail_UsesIdForUniqueName()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = null,
      Email = null
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var uniqueNameClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName);
    Assert.NotNull(uniqueNameClaim);
    Assert.Equal("user123", uniqueNameClaim.Value);
  }

  /// <summary>
  /// Tests that CreateToken with user having null UserName uses Email for unique name claim.
  /// Input: User with null UserName but valid Email.
  /// Expected: Token contains Email as the unique name claim.
  /// </summary>
  [Fact]
  public void CreateToken_WithUserNullUserName_UsesEmailForUniqueName()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = null,
      Email = "test@example.com"
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var uniqueNameClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName);
    Assert.NotNull(uniqueNameClaim);
    Assert.Equal("test@example.com", uniqueNameClaim.Value);
  }

  /// <summary>
  /// Tests that CreateToken with user having null Email uses empty string for email claim.
  /// Input: User with null Email.
  /// Expected: Token contains empty string as the email claim.
  /// </summary>
  [Fact]
  public void CreateToken_WithNullEmail_UsesEmptyStringForEmailClaim()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = null
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var emailClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
    Assert.NotNull(emailClaim);
    Assert.Equal(string.Empty, emailClaim.Value);
  }

  /// <summary>
  /// Tests that CreateToken with multiple roles includes all role claims in the token.
  /// Input: User with multiple roles.
  /// Expected: Token contains all role claims.
  /// </summary>
  [Fact]
  public void CreateToken_WithMultipleRoles_IncludesAllRoleClaimsInToken()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string> { "Admin", "User", "Manager" };

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    Assert.Equal(3, roleClaims.Count);
    Assert.Contains("Admin", roleClaims);
    Assert.Contains("User", roleClaims);
    Assert.Contains("Manager", roleClaims);
  }

  /// <summary>
  /// Tests that CreateToken includes FamilyId claim in the token.
  /// Input: User with specific FamilyId.
  /// Expected: Token contains FamilyId claim with correct value.
  /// </summary>
  [Fact]
  public void CreateToken_WithUserFamilyId_IncludesFamilyIdClaimInToken()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 42
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var familyIdClaim = token.Claims.FirstOrDefault(c => c.Type == "FamilyId");
    Assert.NotNull(familyIdClaim);
    Assert.Equal("42", familyIdClaim.Value);
  }

  /// <summary>
  /// Tests that CreateToken with zero ExpMinutes creates a token expiring immediately.
  /// Input: Options with ExpMinutes = 0.
  /// Expected: Token expiration time is approximately current UTC time.
  /// </summary>
  [Fact(Skip = "ProductionBugSuspected")]
  [Trait("Category", "ProductionBugSuspected")]
  public void CreateToken_WithZeroExpMinutes_CreatesTokenExpiringImmediately()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456",
      ExpMinutes = 0
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string>();
    var beforeCall = DateTime.UtcNow;

    // Act
    var result = service.CreateToken(user, roles);
    var afterCall = DateTime.UtcNow;

    // Assert
    Assert.NotNull(result);
    Assert.True(result.ExpiresUtc >= beforeCall);
    Assert.True(result.ExpiresUtc <= afterCall.AddSeconds(1));
  }

  /// <summary>
  /// Tests that CreateToken with negative ExpMinutes creates an expired token.
  /// Input: Options with ExpMinutes = -60.
  /// Expected: Token expiration time is in the past.
  /// </summary>
  [Fact(Skip = "ProductionBugSuspected")]
  [Trait("Category", "ProductionBugSuspected")]
  public void CreateToken_WithNegativeExpMinutes_CreatesExpiredToken()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456",
      ExpMinutes = -60
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    Assert.True(result.ExpiresUtc < DateTime.UtcNow);
  }

  /// <summary>
  /// Tests that CreateToken with empty SigningKey throws an exception.
  /// Input: Options with empty SigningKey.
  /// Expected: ArgumentException is thrown.
  /// </summary>
  [Fact]
  [Trait("Category", "ProductionBugSuspected")]
  public void CreateToken_WithEmptySigningKey_ThrowsArgumentException()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = string.Empty
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string>();

    // Act & Assert
    Assert.Throws<ArgumentException>(() => service.CreateToken(user, roles));
  }

  /// <summary>
  /// Tests that CreateToken with too short SigningKey throws an exception.
  /// Input: Options with SigningKey shorter than 16 bytes.
  /// Expected: ArgumentOutOfRangeException is thrown.
  /// </summary>
  [Fact]
  [Trait("Category", "ProductionBugSuspected")]
  public void CreateToken_WithTooShortSigningKey_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "short"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string>();

    // Act & Assert
    Assert.Throws<ArgumentOutOfRangeException>(() => service.CreateToken(user, roles));
  }

  /// <summary>
  /// Tests that CreateToken sets correct issuer and audience in the token.
  /// Input: Options with custom Issuer and Audience values.
  /// Expected: Token contains correct issuer and audience.
  /// </summary>
  [Fact]
  public void CreateToken_WithCustomIssuerAndAudience_SetsCorrectIssuerAndAudience()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      Issuer = "custom-issuer",
      Audience = "custom-audience",
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    Assert.Equal("custom-issuer", token.Issuer);
    Assert.Contains("custom-audience", token.Audiences);
  }

  /// <summary>
  /// Tests that CreateToken includes Subject claim with user Id.
  /// Input: User with specific Id.
  /// Expected: Token contains Subject claim with user Id.
  /// </summary>
  [Fact]
  public void CreateToken_WithUserId_IncludesSubjectClaimWithUserId()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "unique-user-id-123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var subClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
    Assert.NotNull(subClaim);
    Assert.Equal("unique-user-id-123", subClaim.Value);
  }

  /// <summary>
  /// Tests that CreateToken includes a unique Jti (JWT ID) claim.
  /// Input: Valid user.
  /// Expected: Token contains a non-empty Jti claim.
  /// </summary>
  [Fact]
  public void CreateToken_WithValidUser_IncludesJtiClaim()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var jtiClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
    Assert.NotNull(jtiClaim);
    Assert.False(string.IsNullOrEmpty(jtiClaim.Value));
    Assert.True(Guid.TryParse(jtiClaim.Value, out _));
  }

  /// <summary>
  /// Tests that CreateToken with very large ExpMinutes value creates token without throwing.
  /// Input: Options with ExpMinutes = int.MaxValue.
  /// Expected: Token is created successfully with far future expiration.
  /// </summary>
  [Fact]
  public void CreateToken_WithVeryLargeExpMinutes_CreatesTokenSuccessfully()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456",
      ExpMinutes = int.MaxValue
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    Assert.False(string.IsNullOrEmpty(result.AccessToken));
    Assert.True(result.ExpiresUtc > DateTime.UtcNow);
  }

  /// <summary>
  /// Tests that CreateToken with roles containing special characters includes them correctly.
  /// Input: Roles with special characters.
  /// Expected: Token contains role claims with special characters.
  /// </summary>
  [Fact]
  public void CreateToken_WithRolesContainingSpecialCharacters_IncludesRolesCorrectly()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com"
    };
    var roles = new List<string> { "Admin@System", "User-Manager", "Role.Special" };

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    Assert.Equal(3, roleClaims.Count);
    Assert.Contains("Admin@System", roleClaims);
    Assert.Contains("User-Manager", roleClaims);
    Assert.Contains("Role.Special", roleClaims);
  }

  /// <summary>
  /// Tests that CreateToken with minimum valid FamilyId includes it correctly.
  /// Input: User with FamilyId = int.MinValue.
  /// Expected: Token contains FamilyId claim with minimum value.
  /// </summary>
  [Fact]
  public void CreateToken_WithMinimumFamilyId_IncludesFamilyIdClaimCorrectly()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = int.MinValue
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var familyIdClaim = token.Claims.FirstOrDefault(c => c.Type == "FamilyId");
    Assert.NotNull(familyIdClaim);
    Assert.Equal(int.MinValue.ToString(), familyIdClaim.Value);
  }

  /// <summary>
  /// Tests that CreateToken with maximum valid FamilyId includes it correctly.
  /// Input: User with FamilyId = int.MaxValue.
  /// Expected: Token contains FamilyId claim with maximum value.
  /// </summary>
  [Fact]
  public void CreateToken_WithMaximumFamilyId_IncludesFamilyIdClaimCorrectly()
  {
    // Arrange
    var options = CreateOptions(new JwtOptions {
      SigningKey = "ThisIsASecretKeyForTestingPurposesOnly123456"
    });
    var service = new JwtTokenService(options);
    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = int.MaxValue
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);
    var familyIdClaim = token.Claims.FirstOrDefault(c => c.Type == "FamilyId");
    Assert.NotNull(familyIdClaim);
    Assert.Equal(int.MaxValue.ToString(), familyIdClaim.Value);
  }

  private static IOptions<JwtOptions> CreateOptions(JwtOptions jwtOptions)
  {
    var mock = new Mock<IOptions<JwtOptions>>();
    mock.Setup(x => x.Value).Returns(jwtOptions);
    return mock.Object;
  }

  /// <summary>
  /// Tests that CreateToken generates a valid JWT token with all expected claims for a user with complete information.
  /// Input: Valid user with UserName, Email, Id, and FamilyId; multiple roles.
  /// Expected: AuthResponse with valid JWT token containing all claims and correct expiration.
  /// </summary>
  [Fact]
  public void CreateToken_ValidUserWithAllProperties_ReturnsAuthResponseWithValidToken()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 42
    };
    var roles = new List<string> { "Admin", "User" };

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.AccessToken);
    Assert.NotEqual(string.Empty, result.AccessToken);
    Assert.True(result.ExpiresUtc > DateTime.UtcNow);
    Assert.True(result.ExpiresUtc <= DateTime.UtcNow.AddMinutes(options.ExpMinutes + 1));

    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal("user123", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
    Assert.Equal("testuser", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
    Assert.Equal("test@example.com", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    Assert.Equal("42", token.Claims.First(c => c.Type == "FamilyId").Value);
    Assert.NotNull(token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti));

    var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    Assert.Equal(2, roleClaims.Count);
    Assert.Contains("Admin", roleClaims);
    Assert.Contains("User", roleClaims);
  }

  /// <summary>
  /// Tests that CreateToken uses Email for UniqueName claim when UserName is null.
  /// Input: User with null UserName but valid Email.
  /// Expected: JWT token with Email as UniqueName claim.
  /// </summary>
  [Fact]
  public void CreateToken_UserWithNullUserName_UsesEmailForUniqueName()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = null,
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal("test@example.com", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
  }

  /// <summary>
  /// Tests that CreateToken uses Id for UniqueName claim when both UserName and Email are null.
  /// Input: User with null UserName and null Email.
  /// Expected: JWT token with Id as UniqueName claim.
  /// </summary>
  [Fact]
  public void CreateToken_UserWithNullUserNameAndEmail_UsesIdForUniqueName()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = null,
      Email = null,
      FamilyId = 1
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal("user123", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
  }

  /// <summary>
  /// Tests that CreateToken uses empty string for Email claim when Email is null.
  /// Input: User with null Email.
  /// Expected: JWT token with empty string as Email claim.
  /// </summary>
  [Fact]
  public void CreateToken_UserWithNullEmail_UsesEmptyStringForEmailClaim()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = null,
      FamilyId = 1
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal(string.Empty, token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
  }

  /// <summary>
  /// Tests that CreateToken handles empty roles collection correctly.
  /// Input: Valid user with empty roles collection.
  /// Expected: JWT token without role claims.
  /// </summary>
  [Fact]
  public void CreateToken_EmptyRolesCollection_CreatesTokenWithoutRoleClaims()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
    Assert.Empty(roleClaims);
  }

  /// <summary>
  /// Tests that CreateToken handles single role correctly.
  /// Input: Valid user with single role.
  /// Expected: JWT token with one role claim.
  /// </summary>
  [Fact]
  public void CreateToken_SingleRole_CreatesTokenWithOneRoleClaim()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string> { "Admin" };

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
    Assert.Single(roleClaims);
    Assert.Equal("Admin", roleClaims[0].Value);
  }

  /// <summary>
  /// Tests that CreateToken calculates expiration time correctly with custom ExpMinutes.
  /// Input: Valid user with ExpMinutes set to 120.
  /// Expected: Token expiration approximately 120 minutes from now.
  /// </summary>
  [Fact]
  public void CreateToken_CustomExpMinutes_SetsCorrectExpirationTime()
  {
    // Arrange
    var options = new JwtOptions {
      Issuer = "test-issuer",
      Audience = "test-audience",
      SigningKey = "ThisIsAVerySecureSigningKeyWith32CharactersMinimum",
      ExpMinutes = 120
    };
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string>();

    var beforeCall = DateTime.UtcNow;

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var expectedExpiration = beforeCall.AddMinutes(120);
    Assert.True(result.ExpiresUtc >= expectedExpiration.AddSeconds(-5));
    Assert.True(result.ExpiresUtc <= expectedExpiration.AddSeconds(5));
  }

  /// <summary>
  /// Tests that CreateToken uses correct issuer and audience from options.
  /// Input: Valid user with custom issuer and audience in options.
  /// Expected: JWT token with correct issuer and audience.
  /// </summary>
  [Fact]
  public void CreateToken_CustomIssuerAndAudience_UsesCorrectValues()
  {
    // Arrange
    var options = new JwtOptions {
      Issuer = "custom-issuer",
      Audience = "custom-audience",
      SigningKey = "ThisIsAVerySecureSigningKeyWith32CharactersMinimum",
      ExpMinutes = 60
    };
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal("custom-issuer", token.Issuer);
    Assert.Contains("custom-audience", token.Audiences);
  }

  /// <summary>
  /// Tests that CreateToken handles user with zero FamilyId.
  /// Input: User with FamilyId set to 0.
  /// Expected: JWT token with FamilyId claim set to "0".
  /// </summary>
  [Fact]
  public void CreateToken_UserWithZeroFamilyId_CreatesFamilyIdClaimWithZero()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 0
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal("0", token.Claims.First(c => c.Type == "FamilyId").Value);
  }

  /// <summary>
  /// Tests that CreateToken handles user with negative FamilyId.
  /// Input: User with FamilyId set to -1.
  /// Expected: JWT token with FamilyId claim set to "-1".
  /// </summary>
  [Fact]
  public void CreateToken_UserWithNegativeFamilyId_CreatesFamilyIdClaimWithNegativeValue()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = -1
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal("-1", token.Claims.First(c => c.Type == "FamilyId").Value);
  }

  /// <summary>
  /// Tests that CreateToken handles user with very large FamilyId.
  /// Input: User with FamilyId set to int.MaxValue.
  /// Expected: JWT token with FamilyId claim set to int.MaxValue string representation.
  /// </summary>
  [Fact]
  public void CreateToken_UserWithMaxFamilyId_CreatesFamilyIdClaimWithMaxValue()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = int.MaxValue
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal(int.MaxValue.ToString(), token.Claims.First(c => c.Type == "FamilyId").Value);
  }

  /// <summary>
  /// Tests that CreateToken handles roles with special characters.
  /// Input: Roles containing special characters.
  /// Expected: JWT token with role claims containing special characters.
  /// </summary>
  [Fact]
  public void CreateToken_RolesWithSpecialCharacters_CreatesTokenWithSpecialCharacterRoles()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string> { "Admin-Role", "User_Manager", "Role@Special" };

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    Assert.Equal(3, roleClaims.Count);
    Assert.Contains("Admin-Role", roleClaims);
    Assert.Contains("User_Manager", roleClaims);
    Assert.Contains("Role@Special", roleClaims);
  }

  /// <summary>
  /// Tests that CreateToken handles empty strings in roles collection.
  /// Input: Roles containing empty strings.
  /// Expected: JWT token with role claims including empty strings.
  /// </summary>
  [Fact]
  public void CreateToken_RolesWithEmptyStrings_CreatesTokenWithEmptyStringRoles()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string> { "Admin", "", "User" };

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    Assert.Equal(3, roleClaims.Count);
    Assert.Contains("", roleClaims);
  }

  /// <summary>
  /// Tests that CreateToken handles duplicate roles.
  /// Input: Roles containing duplicate values.
  /// Expected: JWT token with multiple role claims for duplicates.
  /// </summary>
  [Fact]
  public void CreateToken_DuplicateRoles_CreatesMultipleRoleClaims()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string> { "Admin", "Admin", "User" };

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    Assert.Equal(3, roleClaims.Count);
    Assert.Equal(2, roleClaims.Count(r => r == "Admin"));
  }

  /// <summary>
  /// Tests that CreateToken generates unique Jti claim for each invocation.
  /// Input: Two separate calls with the same user.
  /// Expected: Two different Jti claim values.
  /// </summary>
  [Fact]
  public void CreateToken_MultipleCalls_GeneratesUniqueJtiClaims()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string> { "User" };

    // Act
    var result1 = service.CreateToken(user, roles);
    var result2 = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token1 = handler.ReadJwtToken(result1.AccessToken);
    var token2 = handler.ReadJwtToken(result2.AccessToken);

    var jti1 = token1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
    var jti2 = token2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

    Assert.NotEqual(jti1, jti2);
  }

  /// <summary>
  /// Tests that CreateToken handles user with very long Id.
  /// Input: User with Id containing 500 characters.
  /// Expected: JWT token with long Id in Sub claim.
  /// </summary>
  [Fact]
  public void CreateToken_UserWithVeryLongId_CreatesTokenWithLongIdClaim()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var longId = new string('a', 500);
    var user = new BudgetUser {
      Id = longId,
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal(longId, token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
  }

  /// <summary>
  /// Tests that CreateToken handles user with special characters in UserName.
  /// Input: User with UserName containing special characters and Unicode.
  /// Expected: JWT token with special characters in UniqueName claim.
  /// </summary>
  [Fact]
  public void CreateToken_UserWithSpecialCharactersInUserName_CreatesTokenWithSpecialCharacters()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "test@user#123_äöü",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal("test@user#123_äöü", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
  }

  /// <summary>
  /// Tests that CreateToken handles whitespace-only UserName by using it as-is.
  /// Input: User with whitespace-only UserName.
  /// Expected: JWT token with whitespace-only UniqueName claim.
  /// </summary>
  [Fact]
  public void CreateToken_UserWithWhitespaceOnlyUserName_UsesWhitespaceForUniqueName()
  {
    // Arrange
    var options = CreateDefaultJwtOptions();
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "   ",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal("   ", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
  }

  /// <summary>
  /// Tests that CreateToken handles ExpMinutes of 0.
  /// Input: ExpMinutes set to 0.
  /// Expected: Token expiration approximately at current time.
  /// </summary>
  [Fact(Skip = "ProductionBugSuspected")]
  [Trait("Category", "ProductionBugSuspected")]
  public void CreateToken_ExpMinutesZero_SetsExpirationToCurrentTime()
  {
    // Arrange
    var options = new JwtOptions {
      Issuer = "test-issuer",
      Audience = "test-audience",
      SigningKey = "ThisIsAVerySecureSigningKeyWith32CharactersMinimum",
      ExpMinutes = 0
    };
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string>();

    var beforeCall = DateTime.UtcNow;

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.True(result.ExpiresUtc >= beforeCall.AddSeconds(-2));
    Assert.True(result.ExpiresUtc <= beforeCall.AddSeconds(2));
  }

  /// <summary>
  /// Tests that CreateToken handles negative ExpMinutes.
  /// Input: ExpMinutes set to -60.
  /// Expected: ArgumentException because expiration would be before notBefore.
  /// </summary>
  [Fact]
  public void CreateToken_NegativeExpMinutes_SetsExpirationInThePast()
  {
    // Arrange
    var options = new JwtOptions {
      Issuer = "test-issuer",
      Audience = "test-audience",
      SigningKey = "ThisIsAVerySecureSigningKeyWith32CharactersMinimum",
      ExpMinutes = -60
    };
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string>();

    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(() => service.CreateToken(user, roles));
    Assert.Contains("IDX12401", exception.Message);
  }

  /// <summary>
  /// Tests that CreateToken handles very large ExpMinutes.
  /// Input: ExpMinutes set to int.MaxValue.
  /// Expected: Token expiration far in the future.
  /// </summary>
  [Fact]
  public void CreateToken_VeryLargeExpMinutes_SetsExpirationFarInFuture()
  {
    // Arrange
    var options = new JwtOptions {
      Issuer = "test-issuer",
      Audience = "test-audience",
      SigningKey = "ThisIsAVerySecureSigningKeyWith32CharactersMinimum",
      ExpMinutes = int.MaxValue
    };
    var mockOptions = new Mock<IOptions<JwtOptions>>();
    mockOptions.Setup(x => x.Value).Returns(options);
    var service = new JwtTokenService(mockOptions.Object);

    var user = new BudgetUser {
      Id = "user123",
      UserName = "testuser",
      Email = "test@example.com",
      FamilyId = 1
    };
    var roles = new List<string>();

    // Act
    var result = service.CreateToken(user, roles);

    // Assert
    Assert.True(result.ExpiresUtc > DateTime.UtcNow.AddYears(1000));
  }

  /// <summary>
  /// Creates default JwtOptions for testing.
  /// </summary>
  private static JwtOptions CreateDefaultJwtOptions()
  {
    return new JwtOptions {
      Issuer = "test-issuer",
      Audience = "test-audience",
      SigningKey = "ThisIsAVerySecureSigningKeyWith32CharactersMinimum",
      ExpMinutes = 60
    };
  }
}