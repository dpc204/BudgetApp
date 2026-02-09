using System;
using System.Linq;
using System.Threading.Tasks;
using Budget.Api.Features.Admin.Roles;
using Budget.DB;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace Budget.ApiTests.Admin;

public class RoleEndpointsTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    => new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
      .Options;

  [Fact]
  public async Task GetRoles_ReturnsAllRoles_WithUserCounts()
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

  [Fact]
  public async Task GetRole_WithValidId_ReturnsRole()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var role = new Role 
    {
      Id = 1,
      Name = "Admin", 
      Description = "Administrator", 
      CreatedAt = DateTime.UtcNow,
      ModifiedAt = DateTime.UtcNow.AddDays(-1)
    };
    context.Roles.Add(role);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetRole.Handler(context);

    // Act
    GetRole.Response? result = await handler.Handle(new GetRole.Query(1), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Name.Should().Be("Admin");
    result.Description.Should().Be("Administrator");
    result.Id.Should().Be(1);
    result.ModifiedAt.Should().NotBeNull();
  }

  [Fact]
  public async Task GetRole_WithInvalidId_ReturnsNull()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetRole.Handler(context);

    // Act
    GetRole.Response? result = await handler.Handle(new GetRole.Query(999), CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task CreateRole_CreatesNewRole_WithCorrectTimestamp()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new CreateRole.Handler(context);
    DateTime beforeCreate = DateTime.UtcNow;

    // Act
    CreateRole.Response result = await handler.Handle(
      new CreateRole.Command("PowerUser", "Power User Description"),
      CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be("PowerUser");
    result.Description.Should().Be("Power User Description");

    Role? savedRole = await context.Roles.FindAsync([result.Id], TestContext.Current.CancellationToken);
    savedRole.Should().NotBeNull();
    savedRole!.Name.Should().Be("PowerUser");
    savedRole.CreatedAt.Should().BeOnOrAfter(beforeCreate);
    savedRole.ModifiedAt.Should().BeNull();
  }

  [Fact]
  public async Task UpdateRole_WithValidId_UpdatesRoleAndTimestamp()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    DateTime originalCreatedAt = DateTime.UtcNow.AddDays(-1);
    context.Roles.Add(new Role 
    { 
      Id = 1, 
      Name = "Admin", 
      Description = "Old Description", 
      CreatedAt = originalCreatedAt 
    });
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateRole.Handler(context);
    DateTime beforeUpdate = DateTime.UtcNow;

    // Act
    UpdateRole.Response? result = await handler.Handle(
      new UpdateRole.Command(1, "Admin", "New Description"),
      CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Description.Should().Be("New Description");

    Role? updatedRole = await context.Roles.FindAsync([1], TestContext.Current.CancellationToken);
    updatedRole!.Description.Should().Be("New Description");
    updatedRole.CreatedAt.Should().Be(originalCreatedAt);
    updatedRole.ModifiedAt.Should().NotBeNull();
    updatedRole.ModifiedAt.Should().BeOnOrAfter(beforeUpdate);
  }

  [Fact]
  public async Task UpdateRole_WithInvalidId_ReturnsNull()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new UpdateRole.Handler(context);

    // Act
    UpdateRole.Response? result = await handler.Handle(
      new UpdateRole.Command(999, "Admin", "Description"),
      CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task DeleteRole_WithoutAssignedUsers_DeletesRole()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    context.Roles.Add(new Role { Id = 1, Name = "TestRole", Description = "Test", CreatedAt = DateTime.UtcNow });
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new DeleteRole.Handler(context);

    // Act
    DeleteRole.Response result = await handler.Handle(new DeleteRole.Command(1), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.ErrorMessage.Should().BeNull();
    (await context.Roles.FindAsync([1], TestContext.Current.CancellationToken)).Should().BeNull();
  }

  [Fact]
  public async Task DeleteRole_WithAssignedUsers_ReturnsErrorResponse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var role = new Role { Id = 1, Name = "TestRole", Description = "Test", CreatedAt = DateTime.UtcNow };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Roles.Add(role);
    context.Users.Add(user);
    context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow });
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new DeleteRole.Handler(context);

    // Act
    DeleteRole.Response result = await handler.Handle(new DeleteRole.Command(1), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().Contain("Cannot delete role");
    result.ErrorMessage.Should().Contain("TestRole");
    
    (await context.Roles.FindAsync([1], TestContext.Current.CancellationToken)).Should().NotBeNull();
  }

  [Fact]
  public async Task DeleteRole_WithInvalidId_ReturnsNotFoundError()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new DeleteRole.Handler(context);

    // Act
    DeleteRole.Response result = await handler.Handle(new DeleteRole.Command(999), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().Be("Role not found");
  }
}
