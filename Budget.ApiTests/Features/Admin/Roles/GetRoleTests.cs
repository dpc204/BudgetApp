using System;
using System.Threading;
using System.Threading.Tasks;

using Budget.Api.Features.Admin.Roles;
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

namespace Budget.Api.Features.Admin.Roles.UnitTests;


/// <summary>
/// Unit tests for GetRole.Handler
/// </summary>
public partial class GetRoleTests
{
    /// <summary>
    /// Creates in-memory database options for testing
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
          .Options;
    }

    /// <summary>
    /// Tests that Handle returns null when querying with boundary and invalid ID values.
    /// Input: Various invalid ID values including 0, negative numbers, and extreme boundaries.
    /// Expected: Returns null for all invalid IDs.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public async Task Handle_WithBoundaryAndInvalidIds_ReturnsNull(int invalidId)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Administrator",
            CreatedAt = DateTime.UtcNow
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRole.Handler(context);

        // Act
        GetRole.Response? result = await handler.Handle(new GetRole.Query(invalidId), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle throws OperationCanceledException when cancellation token is already cancelled.
    /// Input: A pre-cancelled cancellation token.
    /// Expected: Throws OperationCanceledException.
    /// </summary>
    [Fact]
    public async Task Handle_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Administrator",
            CreatedAt = DateTime.UtcNow
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRole.Handler(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
          await handler.Handle(new GetRole.Query(1), cts.Token));
    }

    /// <summary>
    /// Tests that Handle returns the correct role when multiple roles exist in the database.
    /// Input: Valid ID with multiple roles present.
    /// Expected: Returns only the role matching the specified ID.
    /// </summary>
    [Fact]
    public async Task Handle_WithMultipleRoles_ReturnsCorrectRole()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var role1 = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Administrator",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow.AddDays(-1)
        };
        var role2 = new Role
        {
            Id = 2,
            Name = "User",
            Description = "Standard User",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow.AddDays(-2)
        };
        var role3 = new Role
        {
            Id = 3,
            Name = "Guest",
            Description = "Guest User",
            CreatedAt = DateTime.UtcNow
        };

        context.Roles.AddRange(role1, role2, role3);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRole.Handler(context);

        // Act
        GetRole.Response? result = await handler.Handle(new GetRole.Query(2), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.Name.Should().Be("User");
        result.Description.Should().Be("Standard User");
        result.ModifiedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that Handle correctly maps all properties including null ModifiedAt.
    /// Input: Valid ID for a role without ModifiedAt value.
    /// Expected: Returns response with null ModifiedAt and all other properties correctly mapped.
    /// </summary>
    [Fact]
    public async Task Handle_WithNullModifiedAt_MapsPropertiesCorrectly()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var createdAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var role = new Role
        {
            Id = 5,
            Name = "TestRole",
            Description = "Test Description",
            CreatedAt = createdAt,
            ModifiedAt = null
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRole.Handler(context);

        // Act
        GetRole.Response? result = await handler.Handle(new GetRole.Query(5), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Name.Should().Be("TestRole");
        result.Description.Should().Be("Test Description");
        result.CreatedAt.Should().Be(createdAt);
        result.ModifiedAt.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle returns null immediately when database is empty.
    /// Input: Valid ID but empty database.
    /// Expected: Returns null without errors.
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsNull()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new GetRole.Handler(context);

        // Act
        GetRole.Response? result = await handler.Handle(new GetRole.Query(1), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
