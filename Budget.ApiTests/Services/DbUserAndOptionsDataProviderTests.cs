using Budget.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Budget.ApiTests.Services;
/// <summary>
/// Unit tests for DbUserAndOptionsDataProvider.LoadUserOptionsAsync method.
/// </summary>
public sealed class DbUserAndOptionsDataProviderTests : IAsyncDisposable
{
  private readonly BudgetContext _context;
  private readonly Mock<ILogger<DbUserAndOptionsDataProvider>> _mockLogger;
  private readonly DbUserAndOptionsDataProvider _provider;
  public DbUserAndOptionsDataProviderTests()
  {
    var options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
    _context = new BudgetContext(options, null);
    _mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    _provider = new DbUserAndOptionsDataProvider(_context, _mockLogger.Object);
  }

  public async ValueTask DisposeAsync()
  {
    await _context.DisposeAsync();
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync returns null when no SavedUserOptions record exists for the userId.
  /// Input: userId with no matching record in database
  /// Expected: Returns null
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_UserNotFound_ReturnsNull()
  {
    // Arrange
    const int nonExistentUserId = 999;
    // Act
    var result = await _provider.LoadUserOptionsAsync(nonExistentUserId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync returns null when JsonOptions property is null.
  /// Input: SavedUserOptions record with null JsonOptions
  /// Expected: Returns null without attempting deserialization
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_NullJsonOptions_ReturnsNull()
  {
    // Arrange
    const int userId = 2;
    _context.SavedUserOptions.Add(new SavedUserOptions { UserId = userId, JsonOptions = null });
    await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await _provider.LoadUserOptionsAsync(userId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
    _mockLogger.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync returns null when JsonOptions is an empty string.
  /// Input: SavedUserOptions record with empty string JsonOptions
  /// Expected: Returns null without attempting deserialization
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_EmptyJsonOptions_ReturnsNull()
  {
    // Arrange
    const int userId = 3;
    _context.SavedUserOptions.Add(new SavedUserOptions { UserId = userId, JsonOptions = string.Empty });
    await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await _provider.LoadUserOptionsAsync(userId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
    _mockLogger.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync handles whitespace-only JSON options.
  /// Input: SavedUserOptions record with whitespace-only JsonOptions
  /// Expected: Attempts deserialization, catches JsonException, logs error, returns null
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_WhitespaceOnlyJsonOptions_LogsErrorAndReturnsNull()
  {
    // Arrange
    const int userId = 4;
    _context.SavedUserOptions.Add(new SavedUserOptions { UserId = userId, JsonOptions = "   " });
    await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await _provider.LoadUserOptionsAsync(userId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
    _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to deserialize user options for user {userId}")), It.IsAny<JsonException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync handles invalid JSON syntax gracefully.
  /// Input: SavedUserOptions record with invalid JSON syntax
  /// Expected: Catches JsonException, logs error with userId, returns null
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_InvalidJsonSyntax_LogsErrorAndReturnsNull()
  {
    // Arrange
    const int userId = 5;
    const string invalidJson = "{this is not valid json}";
    _context.SavedUserOptions.Add(new SavedUserOptions { UserId = userId, JsonOptions = invalidJson });
    await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await _provider.LoadUserOptionsAsync(userId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
    _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to deserialize user options for user {userId}")), It.IsAny<JsonException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync handles malformed JSON gracefully.
  /// Input: SavedUserOptions record with malformed JSON
  /// Expected: Catches JsonException, logs error, returns null
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_MalformedJson_LogsErrorAndReturnsNull()
  {
    // Arrange
    const int userId = 6;
    const string malformedJson = "{\"UserId\":1,\"SelectedCategoryType\":}";
    _context.SavedUserOptions.Add(new SavedUserOptions { UserId = userId, JsonOptions = malformedJson });
    await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await _provider.LoadUserOptionsAsync(userId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
    _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to deserialize user options for user")), It.IsAny<JsonException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync respects cancellation token.
  /// Input: Already cancelled CancellationToken
  /// Expected: Throws OperationCanceledException
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_CancelledToken_ThrowsOperationCanceledException()
  {
    // Arrange
    const int userId = 7;
    var cancelledToken = new CancellationToken(canceled: true);
    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(async () => await _provider.LoadUserOptionsAsync(userId, cancelledToken));
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync handles zero userId value.
  /// Input: userId = 0
  /// Expected: Returns null when no matching record exists
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_ZeroUserId_ReturnsNull()
  {
    // Arrange
    const int userId = 0;
    // Act
    var result = await _provider.LoadUserOptionsAsync(userId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync handles negative userId value.
  /// Input: userId = -1
  /// Expected: Returns null when no matching record exists
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_NegativeUserId_ReturnsNull()
  {
    // Arrange
    const int userId = -1;
    // Act
    var result = await _provider.LoadUserOptionsAsync(userId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync handles int.MinValue userId boundary value.
  /// Input: userId = int.MinValue
  /// Expected: Returns null when no matching record exists
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_MinValueUserId_ReturnsNull()
  {
    // Arrange
    const int userId = int.MinValue;
    // Act
    var result = await _provider.LoadUserOptionsAsync(userId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync handles int.MaxValue userId boundary value.
  /// Input: userId = int.MaxValue
  /// Expected: Returns null when no matching record exists
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_MaxValueUserId_ReturnsNull()
  {
    // Arrange
    const int userId = int.MaxValue;
    // Act
    var result = await _provider.LoadUserOptionsAsync(userId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Tests that LoadUserOptionsAsync does not log errors when user options are not found.
  /// Input: Non-existent userId
  /// Expected: Returns null without logging any errors
  /// </summary>
  [Fact]
  public async Task LoadUserOptionsAsync_NoMatchingRecord_DoesNotLogError()
  {
    // Arrange
    const int userId = 999;
    // Act
    var result = await _provider.LoadUserOptionsAsync(userId, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
    _mockLogger.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
  }

  /// <summary>
  /// Creates a new in-memory database context for testing with a unique database name
  /// </summary>
  private static BudgetContext CreateInMemoryContext()
  {
    var options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
    return new BudgetContext(options, null);
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync returns correct user details with roles when user exists.
  /// Input: Valid user ID for existing user with multiple roles
  /// Expected: UserDetailDto with all user properties and roles populated correctly
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_ExistingUserWithRoles_ReturnsUserDetailWithRoles()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    var user = new User {
      Id = 1,
      Email = "test@example.com",
      FirstName = "John",
      LastName = "Doe",
      FamilyId = 100
    };
    var role1 = new Role {
      Id = 1,
      Name = "Admin"
    };
    var role2 = new Role {
      Id = 2,
      Name = "User"
    };
    var userRole1 = new UserRole {
      UserId = 1,
      RoleId = 1,
      Role = role1,
      User = user,
      AssignedAt = DateTime.UtcNow
    };
    var userRole2 = new UserRole {
      UserId = 1,
      RoleId = 2,
      Role = role2,
      User = user,
      AssignedAt = DateTime.UtcNow
    };
    context.Users.Add(user);
    context.Roles.AddRange(role1, role2);
    context.UserRoles.AddRange(userRole1, userRole2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await provider.LoadUserByIdAsync(1, TestContext.Current.CancellationToken);
    // Assert
    Assert.NotNull(result);
    Assert.Equal(1, result.Id);
    Assert.Equal("TEST@EXAMPLE.COM", result.Email);
    Assert.Equal("John", result.FirstName);
    Assert.Equal("Doe", result.LastName);
    Assert.Equal(100, result.FamilyId);
    Assert.Equal(2, result.Roles.Count);
    Assert.Contains(result.Roles, r => r.Id == 1 && r.Name == "Admin");
    Assert.Contains(result.Roles, r => r.Id == 2 && r.Name == "User");
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync returns user details with empty roles list when user has no roles.
  /// Input: Valid user ID for existing user without any roles
  /// Expected: UserDetailDto with user properties populated and empty roles list
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_ExistingUserWithoutRoles_ReturnsUserDetailWithEmptyRoles()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    var user = new User {
      Id = 2,
      Email = "noroles@example.com",
      FirstName = "Jane",
      LastName = "Smith",
      FamilyId = 200
    };
    context.Users.Add(user);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await provider.LoadUserByIdAsync(2, TestContext.Current.CancellationToken);
    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Id);
    Assert.Equal("NOROLES@EXAMPLE.COM", result.Email);
    Assert.Equal("Jane", result.FirstName);
    Assert.Equal("Smith", result.LastName);
    Assert.Equal(200, result.FamilyId);
    Assert.Empty(result.Roles);
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync returns null when user does not exist.
  /// Input: User ID that does not exist in database
  /// Expected: null
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_NonExistentUser_ReturnsNull()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    // Act
    var result = await provider.LoadUserByIdAsync(999, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync handles zero as user ID correctly.
  /// Input: User ID = 0
  /// Expected: null (no user with ID 0 should exist)
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_ZeroId_ReturnsNull()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    // Act
    var result = await provider.LoadUserByIdAsync(0, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync handles negative user IDs correctly.
  /// Input: User ID = -1
  /// Expected: null (no user with negative ID should exist)
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_NegativeId_ReturnsNull()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    // Act
    var result = await provider.LoadUserByIdAsync(-1, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync handles int.MaxValue as user ID.
  /// Input: User ID = int.MaxValue
  /// Expected: null (no user with this ID exists)
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_MaxIntValue_ReturnsNull()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    // Act
    var result = await provider.LoadUserByIdAsync(int.MaxValue, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync handles int.MinValue as user ID.
  /// Input: User ID = int.MinValue
  /// Expected: null (no user with this ID exists)
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_MinIntValue_ReturnsNull()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    // Act
    var result = await provider.LoadUserByIdAsync(int.MinValue, TestContext.Current.CancellationToken);
    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync respects cancellation token when operation is cancelled.
  /// Input: Valid user ID with cancelled cancellation token
  /// Expected: OperationCanceledException is thrown
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_CancelledToken_ThrowsOperationCanceledException()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    var user = new User {
      Id = 5,
      Email = "cancel@example.com",
      FirstName = "Cancel",
      LastName = "Test",
      FamilyId = 500
    };
    context.Users.Add(user);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var cts = new CancellationTokenSource();
    cts.Cancel();
    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(async () => await provider.LoadUserByIdAsync(5, cts.Token));
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync returns correct user when multiple users exist in database.
  /// Input: Specific user ID when multiple users exist
  /// Expected: UserDetailDto for the requested user only
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_MultipleUsersExist_ReturnsCorrectUser()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    var user1 = new User {
      Id = 10,
      Email = "user1@example.com",
      FirstName = "User",
      LastName = "One",
      FamilyId = 1
    };
    var user2 = new User {
      Id = 20,
      Email = "user2@example.com",
      FirstName = "User",
      LastName = "Two",
      FamilyId = 2
    };
    var user3 = new User {
      Id = 30,
      Email = "user3@example.com",
      FirstName = "User",
      LastName = "Three",
      FamilyId = 3
    };
    context.Users.AddRange(user1, user2, user3);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await provider.LoadUserByIdAsync(20, TestContext.Current.CancellationToken);
    // Assert
    Assert.NotNull(result);
    Assert.Equal(20, result.Id);
    Assert.Equal("USER2@EXAMPLE.COM", result.Email);
    Assert.Equal("User", result.FirstName);
    Assert.Equal("Two", result.LastName);
    Assert.Equal(2, result.FamilyId);
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync correctly handles user with single role.
  /// Input: Valid user ID for user with exactly one role
  /// Expected: UserDetailDto with single role in roles list
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_UserWithSingleRole_ReturnsUserWithOneRole()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    var user = new User {
      Id = 7,
      Email = "singlerole@example.com",
      FirstName = "Single",
      LastName = "Role",
      FamilyId = 700
    };
    var role = new Role {
      Id = 10,
      Name = "Viewer"
    };
    var userRole = new UserRole {
      UserId = 7,
      RoleId = 10,
      Role = role,
      User = user,
      AssignedAt = DateTime.UtcNow
    };
    context.Users.Add(user);
    context.Roles.Add(role);
    context.UserRoles.Add(userRole);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await provider.LoadUserByIdAsync(7, TestContext.Current.CancellationToken);
    // Assert
    Assert.NotNull(result);
    Assert.Single(result.Roles);
    Assert.Equal(10, result.Roles[0].Id);
    Assert.Equal("Viewer", result.Roles[0].Name);
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync handles users with empty string properties correctly.
  /// Input: User with empty string values for Email, FirstName, LastName
  /// Expected: UserDetailDto with empty strings preserved
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_UserWithEmptyStrings_ReturnsUserWithEmptyStrings()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    var user = new User {
      Id = 8,
      Email = string.Empty,
      FirstName = string.Empty,
      LastName = string.Empty,
      FamilyId = 800
    };
    context.Users.Add(user);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await provider.LoadUserByIdAsync(8, TestContext.Current.CancellationToken);
    // Assert
    Assert.NotNull(result);
    Assert.Equal(string.Empty, result.Email);
    Assert.Equal(string.Empty, result.FirstName);
    Assert.Equal(string.Empty, result.LastName);
  }

  /// <summary>
  /// Tests that LoadUserByIdAsync correctly handles special characters in user data.
  /// Input: User with special characters in Email, FirstName, LastName
  /// Expected: UserDetailDto with special characters preserved correctly
  /// </summary>
  [Fact]
  public async Task LoadUserByIdAsync_UserWithSpecialCharacters_ReturnsUserWithSpecialCharacters()
  {
    // Arrange
    await using var context = CreateInMemoryContext();
    var mockLogger = new Mock<ILogger<DbUserAndOptionsDataProvider>>();
    var provider = new DbUserAndOptionsDataProvider(context, mockLogger.Object);
    var user = new User {
      Id = 9,
      Email = "test+special@example.com",
      FirstName = "José",
      LastName = "O'Brien-Smith",
      FamilyId = 900
    };
    context.Users.Add(user);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    // Act
    var result = await provider.LoadUserByIdAsync(9, TestContext.Current.CancellationToken);
    // Assert
    Assert.NotNull(result);
    Assert.Equal("TEST+SPECIAL@EXAMPLE.COM", result.Email);
    Assert.Equal("José", result.FirstName);
    Assert.Equal("O'Brien-Smith", result.LastName);
  }

  /// <summary>
  /// Helper method to create in-memory database options for testing.
  /// </summary>
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
  }
}