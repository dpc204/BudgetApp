using Budget.Api.Features.Admin.Roles;

namespace Budget.ApiTests.Features.Admin.Roles;


/// <summary>
/// Unit tests for GetRoles.Handler
/// </summary>
public partial class GetRolesHandlerTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;
  }

  /// <summary>
  /// Tests that Handle returns all roles with correct user counts when multiple roles exist with varying user counts.
  /// Input: Database with 2 roles, one with 1 user and one with 0 users.
  /// Expected: Response contains 2 roles with accurate user counts.
  /// </summary>
  [Fact]
  public async Task Handle_WithMultipleRolesAndUsers_ReturnsAllRolesWithCorrectUserCounts()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var role1 = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    var role2 = new Role { Id = 2, Name = "User", Description = "Standard User", CreatedAt = DateTime.UtcNow };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Roles.AddRange(role1, role2);
    context.Users.Add(user);
    context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow });
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRoles.Handler(context);

    // Act
    GetRoles.Response result = await handler.Handle(new GetRoles.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Roles.Should().HaveCount(2);

    GetRoles.RoleDto adminRole = result.Roles.Should().ContainSingle(r => r.Name == "Admin").Subject;
    adminRole.UserCount.Should().Be(1);
    adminRole.Description.Should().Be("Administrator");

    GetRoles.RoleDto userRole = result.Roles.Should().ContainSingle(r => r.Name == "User").Subject;
    userRole.UserCount.Should().Be(0);
  }

  /// <summary>
  /// Tests that Handle returns an empty list when no roles exist in the database.
  /// Input: Empty database with no roles.
  /// Expected: Response contains an empty list of roles.
  /// </summary>
  [Fact]
  public async Task Handle_WithNoRoles_ReturnsEmptyList()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetRoles.Handler(context);

    // Act
    GetRoles.Response result = await handler.Handle(new GetRoles.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Roles.Should().BeEmpty();
  }

  /// <summary>
  /// Tests that Handle returns roles with zero user counts when roles exist but have no assigned users.
  /// Input: Database with 3 roles but no UserRole associations.
  /// Expected: Response contains all roles with UserCount of 0.
  /// </summary>
  [Fact]
  public async Task Handle_WithRolesButNoUsers_ReturnsRolesWithZeroUserCount()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var role1 = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    var role2 = new Role { Id = 2, Name = "Editor", Description = "Content Editor", CreatedAt = DateTime.UtcNow };
    var role3 = new Role { Id = 3, Name = "Viewer", Description = "Read Only", CreatedAt = DateTime.UtcNow };

    context.Roles.AddRange(role1, role2, role3);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRoles.Handler(context);

    // Act
    GetRoles.Response result = await handler.Handle(new GetRoles.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Roles.Should().HaveCount(3);
    result.Roles.Should().OnlyContain(r => r.UserCount == 0);
    result.Roles.Should().Contain(r => r.Name == "Admin");
    result.Roles.Should().Contain(r => r.Name == "Editor");
    result.Roles.Should().Contain(r => r.Name == "Viewer");
  }

  /// <summary>
  /// Tests that Handle returns a single role correctly when only one role exists.
  /// Input: Database with a single role with no users.
  /// Expected: Response contains exactly one role with correct properties.
  /// </summary>
  [Fact]
  public async Task Handle_WithSingleRole_ReturnsSingleRoleWithCorrectProperties()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var createdAt = DateTime.UtcNow;
    var modifiedAt = DateTime.UtcNow.AddDays(-1);
    var role = new Role {
      Id = 1,
      Name = "SuperAdmin",
      Description = "Super Administrator",
      CreatedAt = createdAt,
      ModifiedAt = modifiedAt
    };

    context.Roles.Add(role);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRoles.Handler(context);

    // Act
    GetRoles.Response result = await handler.Handle(new GetRoles.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Roles.Should().ContainSingle();

    GetRoles.RoleDto returnedRole = result.Roles.First();
    returnedRole.Id.Should().Be(1);
    returnedRole.Name.Should().Be("SuperAdmin");
    returnedRole.Description.Should().Be("Super Administrator");
    returnedRole.UserCount.Should().Be(0);
    returnedRole.CreatedAt.Should().BeCloseTo(createdAt, TimeSpan.FromSeconds(1));
    returnedRole.ModifiedAt.Should().NotBeNull();
    returnedRole.ModifiedAt!.Value.Should().BeCloseTo(modifiedAt, TimeSpan.FromSeconds(1));
  }

  /// <summary>
  /// Tests that Handle correctly counts multiple users assigned to the same role.
  /// Input: Database with 1 role and 3 users all assigned to that role.
  /// Expected: Response contains the role with UserCount of 3.
  /// </summary>
  [Fact]
  public async Task Handle_WithMultipleUsersForSingleRole_ReturnsCorrectUserCount()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    var user1 = new User { Id = 1, Email = "USER1@TEST.COM", FirstName = "User", LastName = "One", FamilyId = 1 };
    var user2 = new User { Id = 2, Email = "USER2@TEST.COM", FirstName = "User", LastName = "Two", FamilyId = 1 };
    var user3 = new User { Id = 3, Email = "USER3@TEST.COM", FirstName = "User", LastName = "Three", FamilyId = 1 };

    context.Families.Add(family);
    context.Roles.Add(role);
    context.Users.AddRange(user1, user2, user3);
    context.UserRoles.AddRange(
      new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow },
      new UserRole { UserId = 2, RoleId = 1, AssignedAt = DateTime.UtcNow },
      new UserRole { UserId = 3, RoleId = 1, AssignedAt = DateTime.UtcNow }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRoles.Handler(context);

    // Act
    GetRoles.Response result = await handler.Handle(new GetRoles.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Roles.Should().ContainSingle();
    result.Roles.First().UserCount.Should().Be(3);
    result.Roles.First().Name.Should().Be("Admin");
  }

  /// <summary>
  /// Tests that Handle properly maps all properties from Role to RoleDto including nullable ModifiedAt.
  /// Input: Database with roles having both null and non-null ModifiedAt values.
  /// Expected: Response correctly maps all properties including nullable ModifiedAt.
  /// </summary>
  [Fact]
  public async Task Handle_WithNullAndNonNullModifiedAt_MapsPropertiesCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var createdAt = DateTime.UtcNow;
    var modifiedAt = DateTime.UtcNow.AddHours(-2);
    var role1 = new Role { Id = 1, Name = "Role1", Description = "First Role", CreatedAt = createdAt, ModifiedAt = null };
    var role2 = new Role { Id = 2, Name = "Role2", Description = "Second Role", CreatedAt = createdAt, ModifiedAt = modifiedAt };

    context.Roles.AddRange(role1, role2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRoles.Handler(context);

    // Act
    GetRoles.Response result = await handler.Handle(new GetRoles.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Roles.Should().HaveCount(2);

    GetRoles.RoleDto roleWithNullModified = result.Roles.First(r => r.Name == "Role1");
    roleWithNullModified.ModifiedAt.Should().BeNull();

    GetRoles.RoleDto roleWithModified = result.Roles.First(r => r.Name == "Role2");
    roleWithModified.ModifiedAt.Should().NotBeNull();
    roleWithModified.ModifiedAt!.Value.Should().BeCloseTo(modifiedAt, TimeSpan.FromSeconds(1));
  }

  /// <summary>
  /// Tests that Handle respects cancellation token and throws OperationCanceledException when cancelled.
  /// Input: CancellationToken that is already cancelled.
  /// Expected: OperationCanceledException is thrown.
  /// </summary>
  [Fact]
  public async Task Handle_WithCancelledToken_ThrowsOperationCanceledException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    context.Roles.Add(role);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRoles.Handler(context);
    var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource.Cancel();

    // Act
    Func<Task> act = async () => await handler.Handle(new GetRoles.Query(), cancellationTokenSource.Token);

    // Assert
    await act.Should().ThrowAsync<OperationCanceledException>();
  }

  /// <summary>
  /// Tests that Handle correctly handles complex scenario with multiple roles and distributed users.
  /// Input: Database with 4 roles where users are distributed across different roles.
  /// Expected: Response contains all roles with accurate user counts for each.
  /// </summary>
  [Fact]
  public async Task Handle_WithComplexUserRoleDistribution_ReturnsAccurateUserCounts()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var adminRole = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    var editorRole = new Role { Id = 2, Name = "Editor", Description = "Editor", CreatedAt = DateTime.UtcNow };
    var viewerRole = new Role { Id = 3, Name = "Viewer", Description = "Viewer", CreatedAt = DateTime.UtcNow };
    var guestRole = new Role { Id = 4, Name = "Guest", Description = "Guest", CreatedAt = DateTime.UtcNow };

    var user1 = new User { Id = 1, Email = "ADMIN@TEST.COM", FirstName = "Admin", LastName = "User", FamilyId = 1 };
    var user2 = new User { Id = 2, Email = "EDITOR1@TEST.COM", FirstName = "Editor", LastName = "One", FamilyId = 1 };
    var user3 = new User { Id = 3, Email = "EDITOR2@TEST.COM", FirstName = "Editor", LastName = "Two", FamilyId = 1 };
    var user4 = new User { Id = 4, Email = "VIEWER1@TEST.COM", FirstName = "Viewer", LastName = "One", FamilyId = 1 };
    var user5 = new User { Id = 5, Email = "VIEWER2@TEST.COM", FirstName = "Viewer", LastName = "Two", FamilyId = 1 };
    var user6 = new User { Id = 6, Email = "VIEWER3@TEST.COM", FirstName = "Viewer", LastName = "Three", FamilyId = 1 };

    context.Families.Add(family);
    context.Roles.AddRange(adminRole, editorRole, viewerRole, guestRole);
    context.Users.AddRange(user1, user2, user3, user4, user5, user6);
    context.UserRoles.AddRange(
      new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow },
      new UserRole { UserId = 2, RoleId = 2, AssignedAt = DateTime.UtcNow },
      new UserRole { UserId = 3, RoleId = 2, AssignedAt = DateTime.UtcNow },
      new UserRole { UserId = 4, RoleId = 3, AssignedAt = DateTime.UtcNow },
      new UserRole { UserId = 5, RoleId = 3, AssignedAt = DateTime.UtcNow },
      new UserRole { UserId = 6, RoleId = 3, AssignedAt = DateTime.UtcNow }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRoles.Handler(context);

    // Act
    GetRoles.Response result = await handler.Handle(new GetRoles.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Roles.Should().HaveCount(4);

    result.Roles.First(r => r.Name == "Admin").UserCount.Should().Be(1);
    result.Roles.First(r => r.Name == "Editor").UserCount.Should().Be(2);
    result.Roles.First(r => r.Name == "Viewer").UserCount.Should().Be(3);
    result.Roles.First(r => r.Name == "Guest").UserCount.Should().Be(0);
  }

  /// <summary>
  /// Tests that Handle returns roles with special characters in name and description.
  /// Input: Roles with special characters, numbers, and symbols in their properties.
  /// Expected: Response correctly preserves all special characters.
  /// </summary>
  [Fact]
  public async Task Handle_WithSpecialCharactersInRoleProperties_PreservesCharacters()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var role1 = new Role { Id = 1, Name = "Role-123", Description = "Test & Special <Characters>", CreatedAt = DateTime.UtcNow };
    var role2 = new Role { Id = 2, Name = "Role@#$", Description = "Description with \"quotes\" and 'apostrophes'", CreatedAt = DateTime.UtcNow };

    context.Roles.AddRange(role1, role2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRoles.Handler(context);

    // Act
    GetRoles.Response result = await handler.Handle(new GetRoles.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Roles.Should().HaveCount(2);

    result.Roles.Should().Contain(r => r.Name == "Role-123" && r.Description == "Test & Special <Characters>");
    result.Roles.Should().Contain(r => r.Name == "Role@#$" && r.Description == "Description with \"quotes\" and 'apostrophes'");
  }

  /// <summary>
  /// Tests that Handle correctly processes roles with boundary DateTime values.
  /// Input: Roles with DateTime.MinValue and DateTime.MaxValue for CreatedAt.
  /// Expected: Response correctly maps boundary DateTime values.
  /// </summary>
  [Fact]
  public async Task Handle_WithBoundaryDateTimeValues_MapsCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var role1 = new Role { Id = 1, Name = "MinDate", Description = "Role with min date", CreatedAt = DateTime.MinValue };
    var role2 = new Role { Id = 2, Name = "MaxDate", Description = "Role with max date", CreatedAt = DateTime.MaxValue };

    context.Roles.AddRange(role1, role2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRoles.Handler(context);

    // Act
    GetRoles.Response result = await handler.Handle(new GetRoles.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Roles.Should().HaveCount(2);

    GetRoles.RoleDto minDateRole = result.Roles.First(r => r.Name == "MinDate");
    minDateRole.CreatedAt.Should().Be(DateTime.MinValue);

    GetRoles.RoleDto maxDateRole = result.Roles.First(r => r.Name == "MaxDate");
    maxDateRole.CreatedAt.Should().Be(DateTime.MaxValue);
  }

  /// <summary>
  /// Tests that Handle correctly processes roles with boundary integer ID values.
  /// Input: Roles with ID values at integer boundaries (int.MaxValue).
  /// Expected: Response correctly maps boundary ID values.
  /// </summary>
  [Fact]
  public async Task Handle_WithBoundaryIdValues_MapsCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var role1 = new Role { Id = int.MaxValue, Name = "MaxId", Description = "Role with max ID", CreatedAt = DateTime.UtcNow };
    var role2 = new Role { Id = 1, Name = "NormalId", Description = "Role with normal ID", CreatedAt = DateTime.UtcNow };

    context.Roles.AddRange(role1, role2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRoles.Handler(context);

    // Act
    GetRoles.Response result = await handler.Handle(new GetRoles.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Roles.Should().HaveCount(2);
    result.Roles.Should().Contain(r => r.Id == int.MaxValue && r.Name == "MaxId");
    result.Roles.Should().Contain(r => r.Id == 1 && r.Name == "NormalId");
  }
}
