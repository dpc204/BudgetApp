using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Budget.Api.Features.Transactions;
using Budget.DB;
using Budget.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Budget.Api.Features.Transactions.UnitTests;


/// <summary>
/// Unit tests for the InsertTransactions.AddMultipleTransactions method.
/// </summary>
public partial class InsertTransactionsTests
{
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .Options;
    }

    /// <summary>
    /// Tests that AddMultipleTransactions throws ArgumentNullException when the list parameter is null.
    /// Input: null list
    /// Expected: ArgumentNullException
    /// </summary>
    [Trait("Category", "ProductionBugSuspected")]
    public async Task AddMultipleTransactions_NullList_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);

        var service = new InsertTransactions(context, mockCurrentFamilyService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
          await service.AddMultipleTransactions(null!));
    }

    /// <summary>
    /// Tests that AddSingleTransaction with a null command throws ArgumentNullException.
    /// Input: null command.
    /// Expected: ArgumentNullException is thrown.
    /// </summary>
    [Fact]
    [Trait("Category", "ProductionBugSuspected")]
    public async Task AddSingleTransaction_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockFamilyService = new Mock<ICurrentFamilyService>();
        mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var inserter = new InsertTransactions(context, mockFamilyService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
          await inserter.AddSingleTransaction(null!));
    }

    private readonly BudgetContext _context;
    private readonly Mock<ICurrentFamilyService> _mockCurrentFamilyService;

    public InsertTransactionsTests()
    {
        _context = new BudgetContext(CreateInMemoryOptions(), null);
        _mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    /// <summary>
    /// Tests that EndBatchAsync returns early without performing any operations
    /// when the service is not currently in batch mode (_inBatch = false).
    /// This verifies the guard condition at the start of the method.
    /// Expected: Method completes without adding transactions or modifying database.
    /// </summary>
    [Fact]
    public async Task EndBatchAsync_WhenNotInBatch_ReturnsWithoutPerformingOperations()
    {
        // Arrange
        var service = new InsertTransactions(_context, _mockCurrentFamilyService.Object);
        var initialTransactionCount = await _context.Transactions.CountAsync();

        // Act
        await service.EndBatchAsync();

        // Assert
        var finalTransactionCount = await _context.Transactions.CountAsync();
        finalTransactionCount.Should().Be(initialTransactionCount);
    }

    /// <summary>
    /// Tests that EndBatchAsync properly handles the scenario where
    /// BeginBatchAsync is called multiple times before EndBatchAsync.
    /// Since BeginBatchAsync clears transactions on each call, this verifies
    /// that the batch state is managed correctly.
    /// Expected: Method completes successfully without errors.
    /// </summary>
    [Fact]
    public async Task EndBatchAsync_AfterMultipleBeginCalls_CompletesSuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BudgetContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var context = new BudgetContext(options, null);
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
        await service.BeginBatchAsync();
        await service.BeginBatchAsync(); // Second call should return early

        // Act
        Func<Task> act = async () => await service.EndBatchAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that EndBatchAsync can be called on a freshly instantiated service
    /// without any prior BeginBatchAsync call.
    /// Expected: Method returns early without throwing exceptions or performing operations.
    /// </summary>
    [Fact]
    public async Task EndBatchAsync_WithoutPriorBeginBatch_ReturnsWithoutError()
    {
        // Arrange
        var service = new InsertTransactions(_context, _mockCurrentFamilyService.Object);

        // Act
        Func<Task> act = async () => await service.EndBatchAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests the complete batch lifecycle: BeginBatchAsync followed by EndBatchAsync.
    /// Verifies that the batch can be properly started and ended without errors.
    /// Expected: No exceptions, proper state transitions.
    /// </summary>
    [Fact]
    public async Task EndBatchAsync_CompleteBatchLifecycle_CompletesSuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BudgetContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var context = new BudgetContext(options, null);
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        var service = new InsertTransactions(context, mockCurrentFamilyService.Object);

        // Act
        await service.BeginBatchAsync();
        Func<Task> act = async () => await service.EndBatchAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that after EndBatchAsync completes, a new batch can be started
    /// with BeginBatchAsync, verifying that the state is properly reset.
    /// Expected: A new batch can be started after ending the previous one.
    /// </summary>
    [Fact]
    public async Task EndBatchAsync_AllowsNewBatchAfterCompletion_SuccessfullyStartsNewBatch()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BudgetContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var context = new BudgetContext(options, null);
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
        await service.BeginBatchAsync();
        await service.EndBatchAsync();

        // Act
        Func<Task> act = async () => await service.BeginBatchAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that EndBatchAsync returns early without performing any operations when not in batch mode.
    /// This verifies the early return guard clause when _inBatch is false.
    /// Expected result: Method completes without throwing and without performing any database operations.
    /// </summary>
    [Fact]
    public async Task EndBatchAsync_WhenNotInBatch_ShouldReturnEarlyWithoutDatabaseOperations()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockFamilyService = new Mock<ICurrentFamilyService>();
        mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var inserter = new InsertTransactions(context, mockFamilyService.Object);

        // Act
        await inserter.EndBatchAsync();

        // Assert - should complete without error and without adding any transactions
        var transactions = await context.Transactions.ToListAsync();
        transactions.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that EndBatchAsync successfully commits an empty batch when in batch mode with no transactions.
    /// This verifies that the transaction infrastructure is properly executed even with no data.
    /// Expected result: Batch mode is exited (_inBatch becomes false) and transaction is committed successfully.
    /// </summary>
    [Fact]
    public async Task EndBatchAsync_WhenInBatchWithNoTransactions_ShouldCommitEmptyBatch()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BudgetContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var context = new BudgetContext(options, null);
        var mockFamilyService = new Mock<ICurrentFamilyService>();
        mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var inserter = new InsertTransactions(context, mockFamilyService.Object);

        await inserter.BeginBatchAsync();

        // Act
        await inserter.EndBatchAsync();

        // Assert - should complete without error
        var transactions = await context.Transactions.ToListAsync();
        transactions.Should().BeEmpty();

        // Verify we can begin a new batch (proving _inBatch was reset to false)
        await inserter.BeginBatchAsync();
        await inserter.EndBatchAsync();
    }

    /// <summary>
    /// Tests that multiple calls to EndBatchAsync when not in batch mode are handled gracefully.
    /// This verifies idempotent behavior of the early return guard clause.
    /// Expected result: Multiple calls complete without errors or side effects.
    /// </summary>
    [Fact]
    public async Task EndBatchAsync_WhenCalledMultipleTimesNotInBatch_ShouldHandleGracefully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockFamilyService = new Mock<ICurrentFamilyService>();
        mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var inserter = new InsertTransactions(context, mockFamilyService.Object);

        // Act
        await inserter.EndBatchAsync();
        await inserter.EndBatchAsync();
        await inserter.EndBatchAsync();

        // Assert - should complete without error
        var transactions = await context.Transactions.ToListAsync();
        transactions.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that DisposeAsync completes successfully when not in batch mode.
    /// Expected result: The method completes without throwing an exception.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_WhenNotInBatch_ShouldCompleteSuccessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockFamilyService = new Mock<ICurrentFamilyService>();
        mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);

        var inserter = new InsertTransactions(context, mockFamilyService.Object);

        // Act
        var act = async () => await inserter.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that calling DisposeAsync multiple times does not throw an exception.
    /// Expected result: Multiple calls complete successfully without errors.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_WhenCalledMultipleTimes_ShouldNotThrowException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockFamilyService = new Mock<ICurrentFamilyService>();
        mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);

        var inserter = new InsertTransactions(context, mockFamilyService.Object);

        // Act
        await inserter.DisposeAsync();
        var act = async () => await inserter.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that DisposeAsync properly disposes the database context.
    /// Expected result: The context is disposed and cannot be used after DisposeAsync is called.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_ShouldDisposeContext()
    {
        // Arrange
        var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockFamilyService = new Mock<ICurrentFamilyService>();
        mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);

        var inserter = new InsertTransactions(context, mockFamilyService.Object);

        // Act
        await inserter.DisposeAsync();

        // Assert
        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<ObjectDisposedException>("because the context should be disposed");
    }

    /// <summary>
    /// Tests that BeginBatchAsync initializes batch mode successfully when not already in batch.
    /// Input: First call to BeginBatchAsync.
    /// Expected: Method completes without throwing an exception.
    /// </summary>
    [Fact]
    public async Task BeginBatchAsync_WhenNotInBatch_ShouldCompleteSucessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);

        // Act
        Func<Task> act = async () => await insertTransactions.BeginBatchAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that BeginBatchAsync returns early when already in batch mode.
    /// Input: Two consecutive calls to BeginBatchAsync.
    /// Expected: Second call returns early without errors (tests the early return path at lines 43-44).
    /// </summary>
    [Fact]
    public async Task BeginBatchAsync_WhenAlreadyInBatch_ShouldReturnEarly()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);

        // Act - Call BeginBatchAsync twice
        await insertTransactions.BeginBatchAsync();
        Func<Task> act = async () => await insertTransactions.BeginBatchAsync();

        // Assert - Second call should complete without throwing (early return path)
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that BeginBatchAsync can be called multiple times without throwing exceptions.
    /// Input: Multiple consecutive calls to BeginBatchAsync.
    /// Expected: All calls complete successfully demonstrating idempotent behavior.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task BeginBatchAsync_CalledMultipleTimes_ShouldNotThrow(int callCount)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);

        // Act & Assert
        for (int i = 0; i < callCount; i++)
        {
            Func<Task> act = async () => await insertTransactions.BeginBatchAsync();
            await act.Should().NotThrowAsync($"call {i + 1} of {callCount} should not throw");
        }
    }
}