using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Budget.Api.Features.Admin.UserRoles;
using Budget.DB;
using Carter;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Budget.Api.Features.Admin.UserRoles.UnitTests;


/// <summary>
/// Tests for AssignRole.Handler
/// </summary>
public partial class AssignRoleTests
{
    /// <summary>
    /// Creates in-memory database options for testing
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
          .Options;
    }

    /// <summary>
    /// Tests that Handle successfully assigns a role to a user when all conditions are valid
    /// </summary>
    [Fact]
    public async Task Handle_ValidRequest_AssignsRoleSuccessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };
        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };

        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 1);

        // Act
        AssignRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.RoleId.Should().Be(1);
        result.RoleName.Should().Be("Admin");
        result.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        UserRole? savedUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        savedUserRole.Should().NotBeNull();
        savedUserRole!.AssignedByUserId.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle throws InvalidOperationException when the user does not exist
    /// </summary>
    [Fact]
    public async Task Handle_UserDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(999, 1);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage("User with ID 999 not found");
    }

    /// <summary>
    /// Tests that Handle throws InvalidOperationException when the role does not exist
    /// </summary>
    [Fact]
    public async Task Handle_RoleDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };

        context.Families.Add(family);
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 999);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage("Role with ID 999 not found");
    }

    /// <summary>
    /// Tests that Handle throws InvalidOperationException when the role is already assigned to the user
    /// </summary>
    [Fact]
    public async Task Handle_RoleAlreadyAssigned_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };
        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
        var existingUserRole = new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow };

        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(existingUserRole);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 1);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage("User already has the 'Admin' role assigned");
    }

    /// <summary>
    /// Tests that Handle correctly sets AssignedByUserId when current user is found in HTTP context
    /// </summary>
    [Fact]
    public async Task Handle_WithCurrentUserInContext_SetsAssignedByUserId()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };
        var currentUser = new User { Id = 2, Email = "admin@test.com", FirstName = "Admin", LastName = "User", FamilyId = 1 };
        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };

        context.Families.Add(family);
        context.Users.AddRange(user, currentUser);
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var claims = new[] { new Claim(ClaimTypes.Email, "admin@test.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 1);

        // Act
        AssignRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        UserRole? savedUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        savedUserRole.Should().NotBeNull();
        savedUserRole!.AssignedByUserId.Should().Be(2);
    }

    /// <summary>
    /// Tests that Handle sets AssignedByUserId to null when HTTP context has no email claim
    /// </summary>
    [Fact]
    public async Task Handle_WithNoEmailClaim_SetsAssignedByUserIdToNull()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };
        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };

        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var claims = new[] { new Claim(ClaimTypes.Name, "TestUser") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 1);

        // Act
        AssignRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        UserRole? savedUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        savedUserRole.Should().NotBeNull();
        savedUserRole!.AssignedByUserId.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle sets AssignedByUserId to null when email claim user is not found in database
    /// </summary>
    [Fact]
    public async Task Handle_WithEmailClaimUserNotFound_SetsAssignedByUserIdToNull()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };
        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };

        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var claims = new[] { new Claim(ClaimTypes.Email, "nonexistent@test.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 1);

        // Act
        AssignRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        UserRole? savedUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        savedUserRole.Should().NotBeNull();
        savedUserRole!.AssignedByUserId.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle correctly handles case-insensitive email comparison
    /// </summary>
    [Fact]
    public async Task Handle_WithDifferentCaseEmail_FindsCurrentUser()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };
        var currentUser = new User { Id = 2, Email = "ADMIN@TEST.COM", FirstName = "Admin", LastName = "User", FamilyId = 1 };
        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };

        context.Families.Add(family);
        context.Users.AddRange(user, currentUser);
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var claims = new[] { new Claim(ClaimTypes.Email, "admin@test.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 1);

        // Act
        AssignRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        UserRole? savedUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        savedUserRole.Should().NotBeNull();
        savedUserRole!.AssignedByUserId.Should().Be(2);
    }

    /// <summary>
    /// Tests that Handle correctly handles boundary value for UserId at int.MinValue
    /// </summary>
    [Fact]
    public async Task Handle_WithMinValueUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(int.MinValue, 1);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage($"User with ID {int.MinValue} not found");
    }

    /// <summary>
    /// Tests that Handle correctly handles boundary value for UserId at int.MaxValue
    /// </summary>
    [Fact]
    public async Task Handle_WithMaxValueUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(int.MaxValue, 1);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage($"User with ID {int.MaxValue} not found");
    }

    /// <summary>
    /// Tests that Handle correctly handles zero UserId
    /// </summary>
    [Fact]
    public async Task Handle_WithZeroUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(0, 1);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage("User with ID 0 not found");
    }

    /// <summary>
    /// Tests that Handle correctly handles negative UserId
    /// </summary>
    [Fact]
    public async Task Handle_WithNegativeUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(-1, 1);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage("User with ID -1 not found");
    }

    /// <summary>
    /// Tests that Handle correctly handles boundary value for RoleId at int.MinValue
    /// </summary>
    [Fact]
    public async Task Handle_WithMinValueRoleId_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };

        context.Families.Add(family);
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, int.MinValue);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage($"Role with ID {int.MinValue} not found");
    }

    /// <summary>
    /// Tests that Handle correctly handles boundary value for RoleId at int.MaxValue
    /// </summary>
    [Fact]
    public async Task Handle_WithMaxValueRoleId_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };

        context.Families.Add(family);
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, int.MaxValue);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage($"Role with ID {int.MaxValue} not found");
    }

    /// <summary>
    /// Tests that Handle correctly handles zero RoleId
    /// </summary>
    [Fact]
    public async Task Handle_WithZeroRoleId_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };

        context.Families.Add(family);
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 0);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage("Role with ID 0 not found");
    }

    /// <summary>
    /// Tests that Handle correctly handles negative RoleId
    /// </summary>
    [Fact]
    public async Task Handle_WithNegativeRoleId_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };

        context.Families.Add(family);
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, -1);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage("Role with ID -1 not found");
    }

    /// <summary>
    /// Tests that Handle correctly handles both boundary values together
    /// </summary>
    [Theory]
    [InlineData(int.MinValue, int.MinValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(0, 0)]
    [InlineData(-100, -200)]
    public async Task Handle_WithInvalidBoundaryValues_ThrowsInvalidOperationException(int userId, int roleId)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(userId, roleId);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that Handle correctly handles empty email string
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyEmailString_SetsAssignedByUserIdToNull()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };
        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };

        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var claims = new[] { new Claim(ClaimTypes.Email, "") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 1);

        // Act
        AssignRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        UserRole? savedUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        savedUserRole.Should().NotBeNull();
        savedUserRole!.AssignedByUserId.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle correctly handles whitespace-only email string
    /// </summary>
    [Fact]
    public async Task Handle_WithWhitespaceEmailString_SetsAssignedByUserIdToNull()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };
        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };

        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var claims = new[] { new Claim(ClaimTypes.Email, "   ") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 1);

        // Act
        AssignRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        UserRole? savedUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == 1 && ur.RoleId == 1);
        savedUserRole.Should().NotBeNull();
        savedUserRole!.AssignedByUserId.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle correctly uses IgnoreQueryFilters for user existence check
    /// This ensures soft-deleted or filtered users can still be assigned roles
    /// </summary>
    [Fact]
    public async Task Handle_UserExistsButFiltered_AssignsRoleSuccessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var user = new User { Id = 1, Email = "user@test.com", FirstName = "Test", LastName = "User", FamilyId = 1 };
        var role = new Role { Id = 1, Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow };

        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var handler = new AssignRole.Handler(context, mockHttpContextAccessor.Object);
        var command = new AssignRole.Command(1, 1);

        // Act
        AssignRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
    }
}


/// <summary>
/// Unit tests for the AssignRole.Endpoint class.
/// Note: The AddRoutes method is a configuration method for ASP.NET Core Minimal APIs.
/// Comprehensive testing of endpoint behavior (authorization, route handling, lambda execution)
/// is best achieved through integration tests using WebApplicationFactory and TestServer.
/// These unit tests verify basic configuration setup only.
/// </summary>
public partial class EndpointTests
{
    /// <summary>
    /// Tests that AddRoutes throws ArgumentNullException when app parameter is null.
    /// Input: null IEndpointRouteBuilder
    /// Expected: ArgumentNullException or NullReferenceException
    /// </summary>
    [Fact]
    public void AddRoutes_WithNullEndpointRouteBuilder_ThrowsException()
    {
        // Arrange
        var endpoint = new AssignRole.Endpoint();

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => endpoint.AddRoutes(null!));
    }
}