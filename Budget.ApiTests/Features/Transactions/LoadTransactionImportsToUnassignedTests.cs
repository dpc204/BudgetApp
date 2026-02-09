using System;

using Budget.Api.Features.Transactions;
using Budget.DB;
using Budget.Shared.Enums;
using Budget.Shared.Models;
using Carter;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Budget.Api.Features.Transactions.UnitTests;


/// <summary>
/// Unit tests for LoadTransactionImportsToUnassigned.Handler
/// </summary>
public partial class LoadTransactionImportsToUnassignedTests
{
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;
    }

    /// <summary>
    /// Tests that Handle throws InvalidOperationException when Unassigned envelope does not exist
    /// </summary>
    [Fact]
    public async Task Handle_WithMissingUnassignedEnvelope_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockSender = new Mock<ISender>();
        var handler = new LoadTransactionImportsToUnassigned.Handler(context, mockSender.Object);
        var command = new LoadTransactionImportsToUnassigned.Command(AccountId: 5, UserId: 10);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
          async () => await handler.Handle(command, CancellationToken.None));
    }

}
