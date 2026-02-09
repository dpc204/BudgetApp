using Budget.Api.Features.Admin.UserRoles;
using Budget.DB;
using Carter;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Budget.Api.Features.Admin.UserRoles.UnitTests;
/// <summary>
/// Unit tests for RemoveRole.Handler
/// </summary>
public partial class RemoveRoleTests
{
    /// <summary>
    /// Helper method to create in-memory database options
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
    }

    /// <summary>
    /// Tests that Handle returns true and removes the UserRole when it exists in the database
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserRoleExists_ReturnsTrueAndRemovesUserRole()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var family = new Family
        {
            Id = 1,
            Name = "Test Family"
        };
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            FamilyId = 1
        };
        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Administrator",
            CreatedAt = DateTime.UtcNow
        };
        var userRole = new UserRole
        {
            UserId = 1,
            RoleId = 1,
            AssignedAt = DateTime.UtcNow
        };
        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(1, 1);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeTrue();
        UserRole? removedUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        removedUserRole.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle returns false when the UserRole does not exist in the database
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserRoleDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(999, 999);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Handle returns false when UserId exists but RoleId does not match any UserRole
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserIdExistsButRoleIdDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var family = new Family
        {
            Id = 1,
            Name = "Test Family"
        };
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            FamilyId = 1
        };
        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Administrator",
            CreatedAt = DateTime.UtcNow
        };
        var userRole = new UserRole
        {
            UserId = 1,
            RoleId = 1,
            AssignedAt = DateTime.UtcNow
        };
        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(1, 999);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeFalse();
        UserRole? existingUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        existingUserRole.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that Handle returns false when RoleId exists but UserId does not match any UserRole
    /// </summary>
    [Fact]
    public async Task Handle_WhenRoleIdExistsButUserIdDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var family = new Family
        {
            Id = 1,
            Name = "Test Family"
        };
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            FamilyId = 1
        };
        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Administrator",
            CreatedAt = DateTime.UtcNow
        };
        var userRole = new UserRole
        {
            UserId = 1,
            RoleId = 1,
            AssignedAt = DateTime.UtcNow
        };
        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(999, 1);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeFalse();
        UserRole? existingUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        existingUserRole.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that Handle only removes the specific UserRole matching both UserId and RoleId
    /// when multiple UserRoles exist
    /// </summary>
    [Fact]
    public async Task Handle_WhenMultipleUserRolesExist_OnlyRemovesMatchingUserRole()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var family = new Family
        {
            Id = 1,
            Name = "Test Family"
        };
        var user1 = new User
        {
            Id = 1,
            Email = "test1@test.com",
            FirstName = "Test1",
            LastName = "User",
            FamilyId = 1
        };
        var user2 = new User
        {
            Id = 2,
            Email = "test2@test.com",
            FirstName = "Test2",
            LastName = "User",
            FamilyId = 1
        };
        var role1 = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Administrator",
            CreatedAt = DateTime.UtcNow
        };
        var role2 = new Role
        {
            Id = 2,
            Name = "User",
            Description = "Standard User",
            CreatedAt = DateTime.UtcNow
        };
        var userRole1 = new UserRole
        {
            UserId = 1,
            RoleId = 1,
            AssignedAt = DateTime.UtcNow
        };
        var userRole2 = new UserRole
        {
            UserId = 1,
            RoleId = 2,
            AssignedAt = DateTime.UtcNow
        };
        var userRole3 = new UserRole
        {
            UserId = 2,
            RoleId = 1,
            AssignedAt = DateTime.UtcNow
        };
        context.Families.Add(family);
        context.Users.AddRange(user1, user2);
        context.Roles.AddRange(role1, role2);
        context.UserRoles.AddRange(userRole1, userRole2, userRole3);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(1, 1);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeTrue();
        UserRole? removedUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        removedUserRole.Should().BeNull();
        UserRole? userRole2Remaining = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 2);
        userRole2Remaining.Should().NotBeNull();
        UserRole? userRole3Remaining = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 2 && ur.RoleId == 1);
        userRole3Remaining.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Handle with boundary value: zero for both UserId and RoleId
    /// Expected to return false as no such UserRole would typically exist
    /// </summary>
    [Fact]
    public async Task Handle_WithZeroIds_ReturnsFalse()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(0, 0);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests Handle with negative UserId
    /// Expected to return false as no such UserRole would typically exist
    /// </summary>
    [Fact]
    public async Task Handle_WithNegativeUserId_ReturnsFalse()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(-1, 1);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests Handle with negative RoleId
    /// Expected to return false as no such UserRole would typically exist
    /// </summary>
    [Fact]
    public async Task Handle_WithNegativeRoleId_ReturnsFalse()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(1, -1);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests Handle with int.MaxValue for UserId and RoleId
    /// Expected to return false as no such UserRole would typically exist
    /// </summary>
    [Theory]
    [InlineData(int.MaxValue, 1)]
    [InlineData(1, int.MaxValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public async Task Handle_WithMaxValueIds_ReturnsFalse(int userId, int roleId)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(userId, roleId);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests Handle with int.MinValue for UserId and RoleId
    /// Expected to return false as no such UserRole would typically exist
    /// </summary>
    [Theory]
    [InlineData(int.MinValue, 1)]
    [InlineData(1, int.MinValue)]
    [InlineData(int.MinValue, int.MinValue)]
    public async Task Handle_WithMinValueIds_ReturnsFalse(int userId, int roleId)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(userId, roleId);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Handle respects the cancellation token when it's cancelled before execution
    /// Expected to throw OperationCanceledException
    /// </summary>
    [Fact]
    public async Task Handle_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(1, 1);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await handler.Handle(command, cts.Token);
        });
    }

    /// <summary>
    /// Tests that Handle works correctly when database is empty
    /// Expected to return false
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsFalse()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new RemoveRole.Handler(context);
        var command = new RemoveRole.Command(1, 1);
        // Act
        bool result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().BeFalse();
    }
}
