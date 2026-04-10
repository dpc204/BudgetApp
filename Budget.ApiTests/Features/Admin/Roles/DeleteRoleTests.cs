using Budget.Api.Features.Admin.Roles;

namespace Budget.ApiTests.Features.Admin.Roles;


/// <summary>
/// Unit tests for DeleteRole.Handler
/// </summary>
public partial class DeleteRoleHandlerTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
  }

  /// <summary>
  /// Tests that Handle successfully deletes a role when the role exists and has no users assigned.
  /// Input: Valid role ID with no users assigned.
  /// Expected: Returns Response with Success=true, role is removed from database.
  /// </summary>
  [Fact]
  public async Task Handle_WithValidRoleIdAndNoUsers_DeletesRoleSuccessfully()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var role = new Role {
      Id = 1,
      Name = "TestRole",
      Description = "Test Description",
      CreatedAt = DateTime.UtcNow
    };
    context.Roles.Add(role);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new DeleteRole.Handler(context);
    var command = new DeleteRole.Command(1);

    // Act
    DeleteRole.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.ErrorMessage.Should().BeNull();

    Role? deletedRole = await context.Roles.FirstOrDefaultAsync(r => r.Id == 1, TestContext.Current.CancellationToken);
    deletedRole.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle returns error when attempting to delete a non-existent role.
  /// Input: Role ID that doesn't exist in the database.
  /// Expected: Returns Response with Success=false and "Role not found" error message.
  /// </summary>
  [Fact]
  public async Task Handle_WithNonExistentRoleId_ReturnsRoleNotFoundError()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new DeleteRole.Handler(context);
    var command = new DeleteRole.Command(999);

    // Act
    DeleteRole.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().Be("Role not found");
  }

  /// <summary>
  /// Tests that Handle prevents deletion when the role has one user assigned.
  /// Input: Role ID with one user assigned.
  /// Expected: Returns Response with Success=false and detailed error message about assigned users.
  /// </summary>
  [Fact]
  public async Task Handle_WithRoleHavingOneUser_PreventsDeletionWithErrorMessage()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var role = new Role {
      Id = 1,
      Name = "Admin",
      Description = "Administrator",
      CreatedAt = DateTime.UtcNow
    };
    var user = new User {
      Id = 1,
      Email = "test@test.com",
      FirstName = "Test",
      LastName = "User",
      FamilyId = 1
    };

    context.Families.Add(family);
    context.Roles.Add(role);
    context.Users.Add(user);
    context.UserRoles.Add(new UserRole {
      UserId = 1,
      RoleId = 1,
      AssignedAt = DateTime.UtcNow
    });
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new DeleteRole.Handler(context);
    var command = new DeleteRole.Command(1);

    // Act
    DeleteRole.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().Be("Cannot delete role 'Admin' because it has 1 user(s) assigned to it.");

    Role? roleStillExists = await context.Roles.FirstOrDefaultAsync(r => r.Id == 1, TestContext.Current.CancellationToken);
    roleStillExists.Should().NotBeNull();
  }

  /// <summary>
  /// Tests that Handle prevents deletion when the role has multiple users assigned.
  /// Input: Role ID with three users assigned.
  /// Expected: Returns Response with Success=false and detailed error message with correct user count.
  /// </summary>
  [Fact]
  public async Task Handle_WithRoleHavingMultipleUsers_PreventsDeletionWithCorrectCount()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var role = new Role {
      Id = 1,
      Name = "PowerUser",
      Description = "Power User",
      CreatedAt = DateTime.UtcNow
    };
    var user1 = new User { Id = 1, Email = "user1@test.com", FirstName = "User", LastName = "One", FamilyId = 1 };
    var user2 = new User { Id = 2, Email = "user2@test.com", FirstName = "User", LastName = "Two", FamilyId = 1 };
    var user3 = new User { Id = 3, Email = "user3@test.com", FirstName = "User", LastName = "Three", FamilyId = 1 };

    context.Families.Add(family);
    context.Roles.Add(role);
    context.Users.AddRange(user1, user2, user3);
    context.UserRoles.AddRange(
        new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow },
        new UserRole { UserId = 2, RoleId = 1, AssignedAt = DateTime.UtcNow },
        new UserRole { UserId = 3, RoleId = 1, AssignedAt = DateTime.UtcNow }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new DeleteRole.Handler(context);
    var command = new DeleteRole.Command(1);

    // Act
    DeleteRole.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().Be("Cannot delete role 'PowerUser' because it has 3 user(s) assigned to it.");

    Role? roleStillExists = await context.Roles.FirstOrDefaultAsync(r => r.Id == 1, TestContext.Current.CancellationToken);
    roleStillExists.Should().NotBeNull();
  }

  /// <summary>
  /// Tests that Handle returns error when called with zero as role ID.
  /// Input: Role ID = 0.
  /// Expected: Returns Response with Success=false and "Role not found" error message.
  /// </summary>
  [Fact]
  public async Task Handle_WithZeroRoleId_ReturnsRoleNotFoundError()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new DeleteRole.Handler(context);
    var command = new DeleteRole.Command(0);

    // Act
    DeleteRole.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().Be("Role not found");
  }

  /// <summary>
  /// Tests that Handle returns error when called with negative role ID.
  /// Input: Role ID = -1.
  /// Expected: Returns Response with Success=false and "Role not found" error message.
  /// </summary>
  [Fact]
  public async Task Handle_WithNegativeRoleId_ReturnsRoleNotFoundError()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new DeleteRole.Handler(context);
    var command = new DeleteRole.Command(-1);

    // Act
    DeleteRole.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().Be("Role not found");
  }

  /// <summary>
  /// Tests that Handle returns error when called with int.MaxValue as role ID.
  /// Input: Role ID = int.MaxValue.
  /// Expected: Returns Response with Success=false and "Role not found" error message.
  /// </summary>
  [Fact]
  public async Task Handle_WithMaxIntRoleId_ReturnsRoleNotFoundError()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new DeleteRole.Handler(context);
    var command = new DeleteRole.Command(int.MaxValue);

    // Act
    DeleteRole.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().Be("Role not found");
  }

  /// <summary>
  /// Tests that Handle returns error when called with int.MinValue as role ID.
  /// Input: Role ID = int.MinValue.
  /// Expected: Returns Response with Success=false and "Role not found" error message.
  /// </summary>
  [Fact]
  public async Task Handle_WithMinIntRoleId_ReturnsRoleNotFoundError()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new DeleteRole.Handler(context);
    var command = new DeleteRole.Command(int.MinValue);

    // Act
    DeleteRole.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().Be("Role not found");
  }

  /// <summary>
  /// Tests that Handle respects the cancellation token during the delete operation.
  /// Input: Valid role ID with a pre-cancelled cancellation token.
  /// Expected: Throws OperationCanceledException.
  /// </summary>
  [Fact]
  public async Task Handle_WithCancelledToken_ThrowsOperationCanceledException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var role = new Role {
      Id = 1,
      Name = "TestRole",
      Description = "Test Description",
      CreatedAt = DateTime.UtcNow
    };
    context.Roles.Add(role);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new DeleteRole.Handler(context);
    var command = new DeleteRole.Command(1);
    var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource.Cancel();

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        await handler.Handle(command, cancellationTokenSource.Token));
  }

  /// <summary>
  /// Tests that Handle successfully deletes a role with empty UserRoles collection.
  /// Input: Role with explicitly empty UserRoles list.
  /// Expected: Returns Response with Success=true, role is removed from database.
  /// </summary>
  [Fact]
  public async Task Handle_WithEmptyUserRolesCollection_DeletesRoleSuccessfully()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var role = new Role {
      Id = 1,
      Name = "EmptyRole",
      Description = "Role with no users",
      CreatedAt = DateTime.UtcNow,
      UserRoles = []
    };
    context.Roles.Add(role);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new DeleteRole.Handler(context);
    var command = new DeleteRole.Command(1);

    // Act
    DeleteRole.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.ErrorMessage.Should().BeNull();

    Role? deletedRole = await context.Roles.FirstOrDefaultAsync(r => r.Id == 1, TestContext.Current.CancellationToken);
    deletedRole.Should().BeNull();
  }
}
