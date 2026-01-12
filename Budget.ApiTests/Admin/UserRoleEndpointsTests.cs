using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Budget.Api.Features.Admin.UserRoles;
using Budget.DB;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace Budget.ApiTests.Admin;

public class UserRoleEndpointsTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    => new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
      .Options;

  private class TestHttpContextAccessor : IHttpContextAccessor
  {
    public HttpContext? HttpContext { get; set; }
  }

  [Fact]
  public async Task GetUserRoles_ReturnsAllRoles_WithAuditInfo()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var adminRole = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    var userRole = new Role { Id = 2, Name = "User", Description = "Standard User", CreatedAt = DateTime.UtcNow };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    var assignedBy = new User { Id = 2, Email = "ADMIN@TEST.COM", FirstName = "Admin", LastName = "User", FamilyId = 1 };
    
    var assignedAt1 = DateTime.UtcNow.AddDays(-2);
    var assignedAt2 = DateTime.UtcNow.AddDays(-1);
    
    context.Families.Add(family);
    context.Roles.AddRange(adminRole, userRole);
    context.Users.AddRange(user, assignedBy);
    context.UserRoles.AddRange(
      new UserRole { UserId = 1, RoleId = 1, AssignedAt = assignedAt1, AssignedByUserId = 2 },
      new UserRole { UserId = 1, RoleId = 2, AssignedAt = assignedAt2, AssignedByUserId = 2 }
    );
    await context.SaveChangesAsync();

    var handler = new GetUserRoles.Handler(context);

    // Act
    var result = await handler.Handle(new GetUserRoles.Query(1), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.UserId.Should().Be(1);
    result.Roles.Should().HaveCount(2);
    
    var adminAssignment = result.Roles.Should().ContainSingle(r => r.RoleName == "Admin").Subject;
    adminAssignment.RoleId.Should().Be(1);
    adminAssignment.AssignedAt.Should().Be(assignedAt1);
    adminAssignment.AssignedByUserId.Should().Be(2);
    adminAssignment.AssignedByName.Should().Be("Admin User");
  }

  [Fact]
  public async Task GetUserRoles_WithNoRoles_ReturnsEmptyList()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetUserRoles.Handler(context);

    // Act
    var result = await handler.Handle(new GetUserRoles.Query(999), CancellationToken.None);

    // Assert
    result.Roles.Should().BeEmpty();
  }

  [Fact]
  public async Task AssignRole_WithValidData_CreatesAssignment_WithAuditTrail()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    var user = new User { Id = 1, Email = "USER@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    var adminUser = new User { Id = 2, Email = "ADMIN@TEST.COM", FirstName = "Admin", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Roles.Add(role);
    context.Users.AddRange(user, adminUser);
    await context.SaveChangesAsync();

    var httpContextAccessor = new TestHttpContextAccessor
    {
      HttpContext = new DefaultHttpContext
      {
        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
          new Claim(ClaimTypes.Email, "ADMIN@TEST.COM")
        }, "TestAuth"))
      }
    };

    var handler = new AssignRole.Handler(context, httpContextAccessor);
    var beforeAssign = DateTime.UtcNow;

    // Act
    var result = await handler.Handle(new AssignRole.Command(1, 1), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.UserId.Should().Be(1);
    result.RoleId.Should().Be(1);
    result.RoleName.Should().Be("Admin");
    result.AssignedAt.Should().BeOnOrAfter(beforeAssign);
    
    var assignment = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
    assignment.Should().NotBeNull();
    assignment!.AssignedByUserId.Should().Be(2);
  }

  [Fact]
  public async Task AssignRole_WhenAlreadyAssigned_ThrowsException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    var user = new User { Id = 1, Email = "USER@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Roles.Add(role);
    context.Users.Add(user);
    context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow });
    await context.SaveChangesAsync();

    var httpContextAccessor = new TestHttpContextAccessor();
    var handler = new AssignRole.Handler(context, httpContextAccessor);

    // Act
    var act = async () => await handler.Handle(new AssignRole.Command(1, 1), CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*already has the*role assigned*");
  }

  [Fact]
  public async Task AssignRole_WithInvalidUser_ThrowsException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    context.Roles.Add(role);
    await context.SaveChangesAsync();

    var httpContextAccessor = new TestHttpContextAccessor();
    var handler = new AssignRole.Handler(context, httpContextAccessor);

    // Act
    var act = async () => await handler.Handle(new AssignRole.Command(999, 1), CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*User with ID 999 not found*");
  }

  [Fact]
  public async Task RemoveRole_WithValidData_RemovesAssignment()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
    var user = new User { Id = 1, Email = "USER@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Roles.Add(role);
    context.Users.Add(user);
    context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow });
    await context.SaveChangesAsync();

    var handler = new RemoveRole.Handler(context);

    // Act
    var result = await handler.Handle(new RemoveRole.Command(1, 1), CancellationToken.None);

    // Assert
    result.Should().BeTrue();
    (await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1))
      .Should().BeNull();
  }

  [Fact]
  public async Task RemoveRole_WithNonExistentAssignment_ReturnsFalse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new RemoveRole.Handler(context);

    // Act
    var result = await handler.Handle(new RemoveRole.Command(1, 1), CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }
}
