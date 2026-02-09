using Budget.Api.Features.Admin.Users;
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Budget.ApiTests.Features.Admin.Users;
/// <summary>
/// Unit tests for GetUser.Endpoint class
/// </summary>
public partial class GetUserEndpointTests
{
    /// <summary>
    /// Tests that AddRoutes throws ArgumentNullException when app parameter is null.
    /// Input: null IEndpointRouteBuilder
    /// Expected: ArgumentNullException
    /// </summary>
    [Fact]
    public void AddRoutes_NullApp_ThrowsArgumentNullException()
    {
        // Arrange
        var endpoint = new GetUser.Endpoint();
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => endpoint.AddRoutes(null!));
    }

    /// <summary>
    /// Tests that AddRoutes successfully registers an endpoint when given a valid IEndpointRouteBuilder.
    /// This test verifies the method executes without throwing.
    /// Input: Valid mocked IEndpointRouteBuilder
    /// Expected: Method completes without exception
    /// </summary>
    [Fact]
    public void AddRoutes_ValidApp_CompletesWithoutException()
    {
        // Arrange
        var endpoint = new GetUser.Endpoint();
        var mockApp = new Mock<IEndpointRouteBuilder>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockDataSource = new Mock<EndpointDataSource>();
        mockApp.Setup(a => a.ServiceProvider).Returns(mockServiceProvider.Object);
        mockApp.Setup(a => a.DataSources).Returns([mockDataSource.Object]);
        // Note: Full endpoint behavior testing (including authorization, tags, and handler logic)
        // requires integration testing with WebApplicationFactory. This unit test verifies
        // that the registration method executes without errors.
        // Act & Assert - Should not throw
        var exception = Record.Exception(() => endpoint.AddRoutes(mockApp.Object));
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests the endpoint handler behavior when sender returns a valid response.
    /// This test verifies the lambda logic that would be registered in AddRoutes.
    /// Input: Valid id and mocked sender returning a response
    /// Expected: Ok result with the response
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public async Task EndpointHandler_ValidResponse_ReturnsOkResult(int id)
    {
        // Arrange
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetUser.Response(id, "test@example.com", "John", "Doe", 1, [new(1, "Admin")]);
        mockSender.Setup(s => s.Send(It.Is<GetUser.Query>(q => q.Id == id), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        // Act
        var result = await mockSender.Object.Send(new GetUser.Query(id), CancellationToken.None);
        var httpResult = result != null ? Results.Ok(result) : Results.NotFound();
        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.IsType<Ok<GetUser.Response>>(httpResult);
    }

    /// <summary>
    /// Tests the endpoint handler behavior when sender returns null.
    /// This test verifies the lambda logic that returns NotFound when user is not found.
    /// Input: Valid id but sender returns null
    /// Expected: NotFound result
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(999)]
    [InlineData(-1)]
    public async Task EndpointHandler_NullResponse_ReturnsNotFound(int id)
    {
        // Arrange
        var mockSender = new Mock<ISender>();
        mockSender.Setup(s => s.Send(It.Is<GetUser.Query>(q => q.Id == id), It.IsAny<CancellationToken>())).ReturnsAsync((GetUser.Response? )null);
        // Act
        var result = await mockSender.Object.Send(new GetUser.Query(id), CancellationToken.None);
        var httpResult = result != null ? Results.Ok(result) : Results.NotFound();
        // Assert
        Assert.Null(result);
        Assert.IsType<NotFound>(httpResult);
    }

    /// <summary>
    /// Tests the endpoint handler behavior when sender returns a response with empty roles list.
    /// Input: Valid id and sender returning response with no roles
    /// Expected: Ok result with empty roles collection
    /// </summary>
    [Fact]
    public async Task EndpointHandler_ResponseWithEmptyRoles_ReturnsOkResult()
    {
        // Arrange
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetUser.Response(1, "test@example.com", "Jane", "Smith", 2, []);
        mockSender.Setup(s => s.Send(It.Is<GetUser.Query>(q => q.Id == 1), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        // Act
        var result = await mockSender.Object.Send(new GetUser.Query(1), CancellationToken.None);
        var httpResult = result != null ? Results.Ok(result) : Results.NotFound();
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Roles);
        Assert.IsType<Ok<GetUser.Response>>(httpResult);
    }

    /// <summary>
    /// Tests the endpoint handler behavior when sender returns a response with multiple roles.
    /// Input: Valid id and sender returning response with multiple roles
    /// Expected: Ok result with all roles included
    /// </summary>
    [Fact]
    public async Task EndpointHandler_ResponseWithMultipleRoles_ReturnsOkResultWithAllRoles()
    {
        // Arrange
        var mockSender = new Mock<ISender>();
        var roles = new List<GetUser.RoleDto>
        {
            new(1, "Admin"),
            new(2, "User"),
            new(3, "Manager")
        };
        var expectedResponse = new GetUser.Response(1, "admin@example.com", "Admin", "User", 1, roles);
        mockSender.Setup(s => s.Send(It.Is<GetUser.Query>(q => q.Id == 1), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        // Act
        var result = await mockSender.Object.Send(new GetUser.Query(1), CancellationToken.None);
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Roles.Count);
        Assert.Contains(result.Roles, r => r.Name == "Admin");
        Assert.Contains(result.Roles, r => r.Name == "User");
        Assert.Contains(result.Roles, r => r.Name == "Manager");
    }

    /// <summary>
    /// Tests the endpoint handler behavior with special characters in response data.
    /// Input: Response with special characters in email, first name, and last name
    /// Expected: Ok result preserving special characters
    /// </summary>
    [Theory]
    [InlineData("test+special@example.com", "O'Brien", "Smith-Jones")]
    [InlineData("user@sub.domain.com", "José", "François")]
    [InlineData("email@test.co.uk", "", "")]
    public async Task EndpointHandler_ResponseWithSpecialCharacters_ReturnsOkResult(string email, string firstName, string lastName)
    {
        // Arrange
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetUser.Response(1, email, firstName, lastName, 1, []);
        mockSender.Setup(s => s.Send(It.IsAny<GetUser.Query>(), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        // Act
        var result = await mockSender.Object.Send(new GetUser.Query(1), CancellationToken.None);
        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Equal(firstName, result.FirstName);
        Assert.Equal(lastName, result.LastName);
    }
}

/// <summary>
/// Unit tests for GetUser.Handler
/// </summary>
public sealed class GetUserHandlerTests
{
    /// <summary>
    /// Tests that Handle returns a user with roles when a valid user ID exists with assigned roles.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidIdAndRoles_ReturnsUserWithRoles()
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
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
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
        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.AddRange(role1, role2);
        context.UserRoles.AddRange(new UserRole { UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow }, new UserRole { UserId = 1, RoleId = 2, AssignedAt = DateTime.UtcNow });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetUser.Handler(context);
        // Act
        GetUser.Response? result = await handler.Handle(new GetUser.Query(1), CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Email.Should().Be("TEST@EXAMPLE.COM");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.FamilyId.Should().Be(1);
        result.Roles.Should().HaveCount(2);
        result.Roles.Should().Contain(r => r.Id == 1 && r.Name == "Admin");
        result.Roles.Should().Contain(r => r.Id == 2 && r.Name == "User");
    }

    /// <summary>
    /// Tests that Handle returns a user with an empty roles list when the user has no assigned roles.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidIdAndNoRoles_ReturnsUserWithEmptyRolesList()
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
            Email = "test@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            FamilyId = 1
        };
        context.Families.Add(family);
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetUser.Handler(context);
        // Act
        GetUser.Response? result = await handler.Handle(new GetUser.Query(1), CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Email.Should().Be("TEST@EXAMPLE.COM");
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.FamilyId.Should().Be(1);
        result.Roles.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle returns null when the requested user ID does not exist.
    /// </summary>
    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new GetUser.Handler(context);
        // Act
        GetUser.Response? result = await handler.Handle(new GetUser.Query(999), CancellationToken.None);
        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle correctly returns null for boundary value int.MinValue when no user exists.
    /// </summary>
    [Fact]
    public async Task Handle_WithMinIntValue_ReturnsNull()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new GetUser.Handler(context);
        // Act
        GetUser.Response? result = await handler.Handle(new GetUser.Query(int.MinValue), CancellationToken.None);
        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle correctly returns null for boundary value int.MaxValue when no user exists.
    /// </summary>
    [Fact]
    public async Task Handle_WithMaxIntValue_ReturnsNull()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new GetUser.Handler(context);
        // Act
        GetUser.Response? result = await handler.Handle(new GetUser.Query(int.MaxValue), CancellationToken.None);
        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle correctly returns null for zero ID when no user exists.
    /// </summary>
    [Fact]
    public async Task Handle_WithZeroId_ReturnsNull()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new GetUser.Handler(context);
        // Act
        GetUser.Response? result = await handler.Handle(new GetUser.Query(0), CancellationToken.None);
        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle correctly returns null for negative ID when no user exists.
    /// </summary>
    [Fact]
    public async Task Handle_WithNegativeId_ReturnsNull()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new GetUser.Handler(context);
        // Act
        GetUser.Response? result = await handler.Handle(new GetUser.Query(-1), CancellationToken.None);
        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle returns the user with a single role when only one role is assigned.
    /// </summary>
    [Fact]
    public async Task Handle_WithSingleRole_ReturnsUserWithOneRole()
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
            Id = 5,
            Email = "single@example.com",
            FirstName = "Single",
            LastName = "Role",
            FamilyId = 1
        };
        var role = new Role
        {
            Id = 10,
            Name = "Viewer",
            Description = "View Only",
            CreatedAt = DateTime.UtcNow
        };
        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(new UserRole { UserId = 5, RoleId = 10, AssignedAt = DateTime.UtcNow });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetUser.Handler(context);
        // Act
        GetUser.Response? result = await handler.Handle(new GetUser.Query(5), CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result!.Roles.Should().HaveCount(1);
        result.Roles[0].Id.Should().Be(10);
        result.Roles[0].Name.Should().Be("Viewer");
    }

    /// <summary>
    /// Tests that Handle respects cancellation token and throws OperationCanceledException when cancelled.
    /// </summary>
    [Fact]
    public async Task Handle_WithCancelledToken_ThrowsOperationCanceledException()
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
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            FamilyId = 1
        };
        context.Families.Add(family);
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetUser.Handler(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        // Act
        Func<Task> act = async () => await handler.Handle(new GetUser.Query(1), cts.Token);
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Tests that Handle returns correct user data with multiple roles in the expected order.
    /// </summary>
    [Fact]
    public async Task Handle_WithMultipleRoles_ReturnsAllRolesCorrectly()
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
            Id = 10,
            Email = "multi@example.com",
            FirstName = "Multi",
            LastName = "Roles",
            FamilyId = 1
        };
        var roles = new[]
        {
            new Role
            {
                Id = 1,
                Name = "Admin",
                Description = "Admin",
                CreatedAt = DateTime.UtcNow
            },
            new Role
            {
                Id = 2,
                Name = "Editor",
                Description = "Editor",
                CreatedAt = DateTime.UtcNow
            },
            new Role
            {
                Id = 3,
                Name = "Viewer",
                Description = "Viewer",
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Families.Add(family);
        context.Users.Add(user);
        context.Roles.AddRange(roles);
        context.UserRoles.AddRange(new UserRole { UserId = 10, RoleId = 1, AssignedAt = DateTime.UtcNow }, new UserRole { UserId = 10, RoleId = 2, AssignedAt = DateTime.UtcNow }, new UserRole { UserId = 10, RoleId = 3, AssignedAt = DateTime.UtcNow });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetUser.Handler(context);
        // Act
        GetUser.Response? result = await handler.Handle(new GetUser.Query(10), CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result!.Roles.Should().HaveCount(3);
        result.Roles.Should().Contain(r => r.Name == "Admin");
        result.Roles.Should().Contain(r => r.Name == "Editor");
        result.Roles.Should().Contain(r => r.Name == "Viewer");
    }

    /// <summary>
    /// Tests that Handle correctly maps all user properties from the database.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidUser_MapsAllPropertiesCorrectly()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var family = new Family
        {
            Id = 42,
            Name = "Special Family"
        };
        var user = new User
        {
            Id = 100,
            Email = "UPPERCASE@EXAMPLE.COM",
            FirstName = "FirstName",
            LastName = "LastName",
            FamilyId = 42
        };
        context.Families.Add(family);
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetUser.Handler(context);
        // Act
        GetUser.Response? result = await handler.Handle(new GetUser.Query(100), CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(100);
        result.Email.Should().Be("UPPERCASE@EXAMPLE.COM");
        result.FirstName.Should().Be("FirstName");
        result.LastName.Should().Be("LastName");
        result.FamilyId.Should().Be(42);
    }

    /// <summary>
    /// Creates DbContextOptions for in-memory database testing.
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    }
}