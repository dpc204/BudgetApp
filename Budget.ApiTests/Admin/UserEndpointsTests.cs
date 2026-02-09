using System;
using System.Linq;
using System.Threading.Tasks;
using Budget.Api.Features.Admin.Users;
using Budget.DB;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace Budget.ApiTests.Admin;

public class UserEndpointsTests : IntegrationTestBase
{
    private new static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    => new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
      .Options;

  [Fact]
  public async Task GetUsers_ReturnsAllUsers_WithRoles_AcrossFamilies()
  {
    // Arrange
    await using BudgetContext context = GetTestDBContext(1);


    var family1 = new Family { Id = 1, Name = "Family 1" };
    var family2 = new Family { Id = 2, Name = "Family 2" };
    var adminRole = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    var userRole = new Role { Id = 2, Name = "User", Description = "Standard User", CreatedAt = DateTime.UtcNow };
    
    var user1 = new User { Id = 1, Email = "ADMIN@TEST.COM", FirstName = "Admin", LastName = "User", FamilyId = 1 };
    var user2 = new User { Id = 2, Email = "USER@TEST.COM", FirstName = "Regular", LastName = "User", FamilyId = 2 };
    
    context.Families.AddRange(family1, family2);
    context.Roles.AddRange(adminRole, userRole);
    context.Users.AddRange(user1, user2);
    context.UserRoles.AddRange(
      new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow },
      new UserRole { UserId = 2, RoleId = 2, AssignedAt = DateTime.UtcNow }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetUsers.Handler(context);

    // Act
    GetUsers.Response result = await handler.Handle(new GetUsers.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Users.Should().HaveCount(2);

    GetUsers.UserDto adminUser = result.Users.Should().ContainSingle(u => u.Email == "ADMIN@TEST.COM").Subject;
    adminUser.FirstName.Should().Be("Admin");
    adminUser.FamilyId.Should().Be(1);
    adminUser.Roles.Should().ContainSingle().Which.Should().Be("Admin");

    GetUsers.UserDto regularUser = result.Users.Should().ContainSingle(u => u.Email == "USER@TEST.COM").Subject;
    regularUser.FamilyId.Should().Be(2);
    regularUser.Roles.Should().ContainSingle().Which.Should().Be("User");
  }

  [Fact]
  public async Task GetUsers_WithNoRoles_ReturnsUsersWithEmptyRolesList()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var user = new User { Id = 1, Email = "USER@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Users.Add(user);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetUsers.Handler(context);

    // Act
    GetUsers.Response result = await handler.Handle(new GetUsers.Query(), CancellationToken.None);

    // Assert
    result.Users.Should().ContainSingle();
    result.Users[0].Roles.Should().BeEmpty();
  }

  [Fact]
  public async Task GetUser_WithValidId_ReturnsUserWithRoleDetails()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var adminRole = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    var powerUserRole = new Role { Id = 2, Name = "PowerUser", Description = "Power User", CreatedAt = DateTime.UtcNow };
    var user = new User { Id = 1, Email = "ADMIN@TEST.COM", FirstName = "Admin", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Roles.AddRange(adminRole, powerUserRole);
    context.Users.Add(user);
    context.UserRoles.AddRange(
      new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow },
      new UserRole { UserId = 1, RoleId = 2, AssignedAt = DateTime.UtcNow }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetUser.Handler(context);

    // Act
    GetUser.Response? result = await handler.Handle(new GetUser.Query(1), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Email.Should().Be("ADMIN@TEST.COM");
    result.FirstName.Should().Be("Admin");
    result.LastName.Should().Be("User");
    result.FamilyId.Should().Be(1);
    result.Roles.Should().HaveCount(2);
    result.Roles.Should().Contain(r => r.Name == "Admin" && r.Id == 1);
    result.Roles.Should().Contain(r => r.Name == "PowerUser" && r.Id == 2);
  }

  [Fact]
  public async Task GetUser_WithInvalidId_ReturnsNull()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetUser.Handler(context);

    // Act
    GetUser.Response? result = await handler.Handle(new GetUser.Query(999), CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task UpdateUser_WithValidId_UpdatesAllFields()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family1 = new Family { Id = 1, Name = "Family 1" };
    var family2 = new Family { Id = 2, Name = "Family 2" };
    var user = new User 
    { 
      Id = 1, 
      Email = "OLD@TEST.COM", 
      FirstName = "Old", 
      LastName = "Name", 
      FamilyId = 1 
    };
    
    context.Families.AddRange(family1, family2);
    context.Users.Add(user);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateUser.Handler(context);

    // Act
    UpdateUser.Response? result = await handler.Handle(
      new UpdateUser.Command(1, "NEW@TEST.COM", "New", "Name", 2),
      CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Email.Should().Be("NEW@TEST.COM");
    result.FirstName.Should().Be("New");
    result.LastName.Should().Be("Name");
    result.FamilyId.Should().Be(2);

    User updatedUser = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == 1, TestContext.Current.CancellationToken);
    updatedUser.Email.Should().Be("NEW@TEST.COM");
    updatedUser.FirstName.Should().Be("New");
    updatedUser.LastName.Should().Be("Name");
    updatedUser.FamilyId.Should().Be(2);
  }

  [Fact]
  public async Task UpdateUser_WithInvalidId_ReturnsNull()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new UpdateUser.Handler(context);

    // Act
    UpdateUser.Response? result = await handler.Handle(
      new UpdateUser.Command(999, "TEST@TEST.COM", "Test", "User", 1),
      CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetUsers_OrdersByEmail_Ascending()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    context.Families.Add(family);
    context.Users.AddRange(
      new User { Id = 1, Email = "ZEBRA@TEST.COM", FirstName = "Z", LastName = "User", FamilyId = 1 },
      new User { Id = 2, Email = "ALPHA@TEST.COM", FirstName = "A", LastName = "User", FamilyId = 1 },
      new User { Id = 3, Email = "MIKE@TEST.COM", FirstName = "M", LastName = "User", FamilyId = 1 }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetUsers.Handler(context);

    // Act
    GetUsers.Response result = await handler.Handle(new GetUsers.Query(), CancellationToken.None);

    // Assert
    result.Users.Should().HaveCount(3);
    result.Users[0].Email.Should().Be("ALPHA@TEST.COM");
    result.Users[1].Email.Should().Be("MIKE@TEST.COM");
    result.Users[2].Email.Should().Be("ZEBRA@TEST.COM");
  }
}

