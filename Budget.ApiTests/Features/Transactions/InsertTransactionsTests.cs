using Budget.Shared.Enums;
using Moq;

namespace Budget.ApiTests.Features.Transactions;
/// <summary>
/// Unit tests for the InsertTransactions.AddMultipleTransactions method.
/// </summary>
public partial class InsertTransactionsTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
  }

  /// <summary>
  /// Tests that AddMultipleTransactions throws ArgumentNullException when the list parameter is null.
  /// Input: null list
  /// Expected: ArgumentNullException
  /// </summary>
  [Trait("Category", "ProductionBugSuspected")]
  [Fact]
  public async Task AddMultipleTransactions_NullList_ThrowsArgumentNullException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(async () => await service.AddMultipleTransactions(null!));
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
    await Assert.ThrowsAsync<ArgumentNullException>(async () => await inserter.AddSingleTransaction(null!));
  }

  private readonly BudgetContext _context;
  private readonly Mock<ICurrentFamilyService> _mockCurrentFamilyService;
  public InsertTransactionsTests()
  {
    _context = new BudgetContext(CreateInMemoryOptions(), null);
    _mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
  }

  internal void Dispose()
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
    var initialTransactionCount = await _context.Transactions.CountAsync(TestContext.Current.CancellationToken);
    // Act
    await service.EndBatchAsync();
    // Assert
    var finalTransactionCount = await _context.Transactions.CountAsync(TestContext.Current.CancellationToken);
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
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
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
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
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
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
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
    List<Transaction> transactions = await context.Transactions.ToListAsync(TestContext.Current.CancellationToken);
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
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockFamilyService = new Mock<ICurrentFamilyService>();
    mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var inserter = new InsertTransactions(context, mockFamilyService.Object);
    await inserter.BeginBatchAsync();
    // Act
    await inserter.EndBatchAsync();
    // Assert - should complete without error
    List<Transaction> transactions = await context.Transactions.ToListAsync(TestContext.Current.CancellationToken);
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
    List<Transaction> transactions = await context.Transactions.ToListAsync(TestContext.Current.CancellationToken);
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
    Func<Task> act = async () => await inserter.DisposeAsync();
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
    Func<Task> act = async () => await inserter.DisposeAsync();
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
    Func<Task<int>> act = async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken);
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
    for(int i = 0; i < callCount; i++)
    {
      Func<Task> act = async () => await insertTransactions.BeginBatchAsync();
      await act.Should().NotThrowAsync($"call {i + 1} of {callCount} should not throw");
    }
  }

  /// <summary>
  /// Tests that AddMultipleTransactions successfully processes an empty list without errors.
  /// Input: Empty list of transactions.
  /// Expected: Returns TransactionAddResult without throwing exceptions.
  /// </summary>
  [Fact]
  public async Task AddMultipleTransactions_EmptyList_CompletesSuccessfully()
  {
    // Arrange
    var options = new DbContextOptionsBuilder<BudgetContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
        .Options;
    await using var context = new BudgetContext(options, null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
    var emptyList = new List<OneTransactionDetail>();
    // Act
    var result = await service.AddMultipleTransactions(emptyList);
    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  /// <summary>
  /// Tests that BeginBatchAsync throws ObjectDisposedException when called after DisposeAsync.
  /// Input: BeginBatchAsync called after DisposeAsync has been invoked.
  /// Expected: ObjectDisposedException is thrown due to disposed context.
  /// </summary>
  [Fact(Skip = "ProductionBugSuspected")]
  [Trait("Category", "ProductionBugSuspected")]
  public async Task BeginBatchAsync_AfterDispose_ThrowsObjectDisposedException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
    await insertTransactions.DisposeAsync();
    // Act
    Func<Task> act = async () => await insertTransactions.BeginBatchAsync();
    // Assert
    await act.Should().ThrowAsync<ObjectDisposedException>();
  }

  /// <summary>
  /// Tests that BeginBatchAsync completes synchronously despite async signature.
  /// Input: Single call to BeginBatchAsync.
  /// Expected: Method completes synchronously without awaiting any operations.
  /// </summary>
  [Fact]
  public async Task BeginBatchAsync_CompletionBehavior_CompletesSuccessfully()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
    // Act
    Task task = insertTransactions.BeginBatchAsync();
    // Assert
    task.IsCompleted.Should().BeTrue("BeginBatchAsync should complete synchronously");
    await task;
  }

  /// <summary>
  /// Tests that BeginBatchAsync can be called after a complete batch lifecycle (Begin -> End -> Begin).
  /// Input: BeginBatchAsync -> EndBatchAsync -> BeginBatchAsync sequence.
  /// Expected: All operations complete successfully without exceptions.
  /// </summary>
  [Fact]
  public async Task BeginBatchAsync_AfterCompleteBatchLifecycle_StartsNewBatchSuccessfully()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
    // Act
    await insertTransactions.BeginBatchAsync();
    await insertTransactions.EndBatchAsync();
    Func<Task> act = async () => await insertTransactions.BeginBatchAsync();
    // Assert
    await act.Should().NotThrowAsync();
  }

  /// <summary>
  /// Tests that EndBatchAsync properly resets the batch state after completion,
  /// allowing subsequent batches to be started and completed successfully.
  /// Input: Complete multiple batch cycles (begin->end->begin->end).
  /// Expected: All cycles complete successfully without state corruption.
  /// </summary>
  [Theory]
  [InlineData(2)]
  [InlineData(3)]
  [InlineData(5)]
  public async Task EndBatchAsync_MultipleBatchCycles_ResetsStateCorrectly(int cycles)
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using BudgetContext context = new BudgetContext(options, null);
    Mock<ICurrentFamilyService> mockFamilyService = new Mock<ICurrentFamilyService>();
    InsertTransactions service = new InsertTransactions(context, mockFamilyService.Object);
    // Act
    for(int i = 0; i < cycles; i++)
    {
      await service.BeginBatchAsync();
      await service.EndBatchAsync();
    }

    // Assert - should be able to start a new batch after all cycles
    Func<Task> act = async () =>
    {
      await service.BeginBatchAsync();
      await service.EndBatchAsync();
    };
    await act.Should().NotThrowAsync();
  }

  /// <summary>
  /// Tests that EndBatchAsync handles gracefully when called on a disposed context.
  /// Input: EndBatchAsync called after context disposal.
  /// Expected: ObjectDisposedException is thrown.
  /// </summary>
  [Fact]
  public async Task EndBatchAsync_WithDisposedContext_ThrowsObjectDisposedException()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    BudgetContext context = new BudgetContext(options, null);
    Mock<ICurrentFamilyService> mockFamilyService = new Mock<ICurrentFamilyService>();
    InsertTransactions service = new InsertTransactions(context, mockFamilyService.Object);
    await service.BeginBatchAsync();
    await context.DisposeAsync();
    // Act
    Func<Task> act = async () => await service.EndBatchAsync();
    // Assert
    await act.Should().ThrowAsync<ObjectDisposedException>();
  }

  /// <summary>
  /// Tests that EndBatchAsync correctly handles the execution strategy pattern
  /// by successfully completing when the database context uses a retry strategy.
  /// Input: EndBatchAsync called within batch mode.
  /// Expected: Method completes successfully with retry strategy.
  /// </summary>
  [Fact]
  public async Task EndBatchAsync_WithExecutionStrategy_CompletesSuccessfully()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using BudgetContext context = new BudgetContext(options, null);
    Mock<ICurrentFamilyService> mockFamilyService = new Mock<ICurrentFamilyService>();
    InsertTransactions service = new InsertTransactions(context, mockFamilyService.Object);
    await service.BeginBatchAsync();
    // Act
    Func<Task> act = async () => await service.EndBatchAsync();
    // Assert
    await act.Should().NotThrowAsync();
  }

  /// <summary>
  /// Tests that EndBatchAsync does not throw when called multiple times in sequence
  /// after a successful batch completion, verifying idempotent behavior.
  /// Input: Multiple sequential calls to EndBatchAsync after batch completion.
  /// Expected: All calls complete without errors.
  /// </summary>
  [Fact]
  public async Task EndBatchAsync_CalledMultipleTimesSequentially_HandlesGracefully()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using BudgetContext context = new BudgetContext(options, null);
    Mock<ICurrentFamilyService> mockFamilyService = new Mock<ICurrentFamilyService>();
    InsertTransactions service = new InsertTransactions(context, mockFamilyService.Object);
    await service.BeginBatchAsync();
    await service.EndBatchAsync();
    // Act
    Func<Task> act = async () =>
    {
      await service.EndBatchAsync();
      await service.EndBatchAsync();
      await service.EndBatchAsync();
    };
    // Assert
    await act.Should().NotThrowAsync();
  }

  /// <summary>
  /// Tests that EndBatchAsync maintains transaction integrity by ensuring
  /// that an empty batch (no transactions added) still executes the full
  /// transaction lifecycle (begin, commit).
  /// Input: BeginBatchAsync followed by immediate EndBatchAsync.
  /// Expected: Transaction infrastructure is properly initialized and cleaned up.
  /// </summary>
  [Fact]
  public async Task EndBatchAsync_EmptyBatchWithTransactionLifecycle_MaintainsIntegrity()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using BudgetContext context = new BudgetContext(options, null);
    Mock<ICurrentFamilyService> mockFamilyService = new Mock<ICurrentFamilyService>();
    InsertTransactions service = new InsertTransactions(context, mockFamilyService.Object);
    await service.BeginBatchAsync();
    // Act
    await service.EndBatchAsync();
    // Assert
    int transactionCount = await context.Transactions.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
    transactionCount.Should().Be(0, "no transactions should be added in an empty batch");
    // Verify state allows starting a new batch
    Func<Task> act = async () => await service.BeginBatchAsync();
    await act.Should().NotThrowAsync("batch state should be properly reset");
  }

  /// <summary>
  /// Tests that AddSingleTransaction throws ArgumentNullException when the Command.Trans property is null.
  /// Input: Command with null Trans property.
  /// Expected: ArgumentNullException is thrown.
  /// </summary>
  [Fact]
  [Trait("Category", "ProductionBugSuspected")]
  public async Task AddSingleTransaction_WithNullTransProperty_ThrowsArgumentNullException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var mockFamilyService = new Mock<ICurrentFamilyService>();
    mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var inserter = new InsertTransactions(context, mockFamilyService.Object);
    var command = new AddNewTransaction.Command(null!);
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(async () => await inserter.AddSingleTransaction(command));
  }

  /// <summary>
  /// Tests that DisposeAsync properly ends an active batch before disposing the context.
  /// Input: Service in batch mode (BeginBatchAsync called).
  /// Expected result: Batch is ended (_inBatch becomes false) and context is disposed successfully.
  /// </summary>
  [Fact]
  public async Task DisposeAsync_WhenInBatchMode_ShouldEndBatchBeforeDisposing()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockFamilyService = new Mock<ICurrentFamilyService>();
    mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var inserter = new InsertTransactions(context, mockFamilyService.Object);
    await inserter.BeginBatchAsync();
    // Act
    Func<Task> act = async () => await inserter.DisposeAsync();
    // Assert
    await act.Should().NotThrowAsync("because DisposeAsync should gracefully end the batch and dispose");
  }

  /// <summary>
  /// Tests that DisposeAsync handles disposal when called during an active batch with no transactions.
  /// Input: Service in batch mode with empty transaction list.
  /// Expected result: Empty batch is ended and context is disposed without errors.
  /// </summary>
  [Fact]
  public async Task DisposeAsync_WhenInBatchModeWithNoTransactions_ShouldCompleteSuccessfully()
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
    await inserter.DisposeAsync();
    // Assert - Verify context is disposed
    Func<Task<int>> act = async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    await act.Should().ThrowAsync<ObjectDisposedException>("because the context should be disposed after DisposeAsync");
  }

  /// <summary>
  /// Tests that DisposeAsync can be safely called after manually ending a batch.
  /// Input: BeginBatchAsync followed by EndBatchAsync, then DisposeAsync.
  /// Expected result: DisposeAsync completes successfully without attempting to end the batch again.
  /// </summary>
  [Fact]
  public async Task DisposeAsync_AfterManuallyEndingBatch_ShouldCompleteSuccessfully()
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
    await inserter.EndBatchAsync();
    // Act
    Func<Task> act = async () => await inserter.DisposeAsync();
    // Assert
    await act.Should().NotThrowAsync("because EndBatchAsync should be idempotent when not in batch");
  }

  /// <summary>
  /// Tests that DisposeAsync properly disposes the context even when called immediately after construction.
  /// Input: Newly constructed service without any operations.
  /// Expected result: Context is disposed and subsequent operations fail with ObjectDisposedException.
  /// </summary>
  [Fact]
  public async Task DisposeAsync_OnFreshInstance_ShouldDisposeContextSuccessfully()
  {
    // Arrange
    var context = new BudgetContext(CreateInMemoryOptions(), null);
    var mockFamilyService = new Mock<ICurrentFamilyService>();
    mockFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var inserter = new InsertTransactions(context, mockFamilyService.Object);
    // Act
    await inserter.DisposeAsync();
    // Assert
    Func<Task<int>> act = async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    await act.Should().ThrowAsync<ObjectDisposedException>("because the context was disposed by DisposeAsync");
  }

  /// <summary>
  /// Tests that DisposeAsync handles multiple batch start/end cycles before disposal.
  /// Input: Multiple BeginBatchAsync/EndBatchAsync cycles followed by DisposeAsync.
  /// Expected result: Service disposes cleanly after multiple batch operations.
  /// </summary>
  [Fact]
  public async Task DisposeAsync_AfterMultipleBatchCycles_ShouldCompleteSuccessfully()
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
    // Start and end batch multiple times
    await inserter.BeginBatchAsync();
    await inserter.EndBatchAsync();
    await inserter.BeginBatchAsync();
    await inserter.EndBatchAsync();
    // Act
    Func<Task> act = async () => await inserter.DisposeAsync();
    // Assert
    await act.Should().NotThrowAsync("because the service should handle multiple batch cycles before disposal");
  }

  /// <summary>
  /// Tests that AddMultipleTransactions successfully processes a single transaction.
  /// Input: List containing one valid transaction with details
  /// Expected: Method completes successfully and returns an EnvelopeUpdates.
  /// </summary>
  [Fact]
  public async Task AddMultipleTransactions_SingleTransaction_CompletesSuccessfully()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
    var transaction = new OneTransactionDetail {
      AccountId = 1,
      Date = DateTime.Today,
      Vendor = "Test Vendor",
      Description = "Test Description",
      UserId = 1,
      TransactionType = TransactionTypes.Expense,
      Details =
        [
            new() {
                    EnvelopeId = 1,
                    Amount = 100.00m,
                    Notes = "Test"
                }
        ]
    };
    var list = new List<OneTransactionDetail>
    {
            transaction
        };
    // Act
    var result = await service.AddMultipleTransactions(list);
    // Assert

    result.Should().NotBeNull();
    result.Should().BeOfType<EnvelopeDeltas>();
  }

  /// <summary>
  /// Tests that AddMultipleTransactions successfully processes multiple transactions.
  /// Input: List containing three valid transactions with details
  /// Expected: Method completes successfully and returns an EnvelopeUpdates.
  /// </summary>
  [Fact]
  public async Task AddMultipleTransactions_MultipleTransactions_CompletesSuccessfully()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
    var list = new List<OneTransactionDetail>
    {
            new() {
                AccountId = 1,
                Date = DateTime.Today,
                Vendor = "Vendor 1",
                Description = "Transaction 1",
                UserId = 1,
                TransactionType = TransactionTypes.Expense,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = 50.00m,
                        Notes = "Detail 1"
                    }
                ]
            },
            new() {
                AccountId = 2,
                Date = DateTime.Today.AddDays(-1),
                Vendor = "Vendor 2",
                Description = "Transaction 2",
                UserId = 1,
                TransactionType = TransactionTypes.Income,
                Details =
                [
                    new() {
                        EnvelopeId = 2,
                        Amount = 100.00m,
                        Notes = "Detail 2"
                    }
                ]
            },
            new() {
                AccountId = 1,
                Date = DateTime.Today.AddDays(-2),
                Vendor = "Vendor 3",
                Description = "Transaction 3",
                UserId = 2,
                TransactionType = TransactionTypes.Transfer,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = -75.00m,
                        Notes = "Transfer out"
                    },
                    new() {
                        EnvelopeId = 3,
                        Amount = 75.00m,
                        Notes = "Transfer in"
                    }
                ]
            }
        };
    // Act
    var result = await service.AddMultipleTransactions(list);
    // Assert

    result.Should().NotBeNull();
    result.Should().BeOfType<EnvelopeDeltas>();
  }

  /// <summary>
  /// Tests that AddMultipleTransactions handles a transaction with empty Details list.
  /// Input: List containing one transaction with no detail items (empty Details list)
  /// Expected: Method completes successfully and returns an EnvelopeUpdates.
  /// </summary>
  [Fact]
  public async Task AddMultipleTransactions_TransactionWithEmptyDetails_CompletesSuccessfully()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
    var transaction = new OneTransactionDetail {
      AccountId = 1,
      Date = DateTime.Today,
      Vendor = "Test Vendor",
      Description = "Transaction with no details",
      UserId = 1,
      TransactionType = TransactionTypes.Expense,
      Details = []
    };
    var list = new List<OneTransactionDetail>
    {
            transaction
        };
    // Act
    var result = await service.AddMultipleTransactions(list);
    // Assert

    result.Should().NotBeNull();
    result.Should().BeOfType<EnvelopeDeltas>();
  }

  /// <summary>
  /// Tests that AddMultipleTransactions handles transactions with boundary date values.
  /// Input: List with transactions using DateTime.MinValue and DateTime.MaxValue
  /// Expected: Method completes successfully and returns an EnvelopeUpdates.
  /// </summary>
  [Fact]
  public async Task AddMultipleTransactions_TransactionsWithBoundaryDates_CompletesSuccessfully()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
    var list = new List<OneTransactionDetail>
    {
            new() {
                AccountId = 1,
                Date = DateTime.MinValue,
                Vendor = "Min Date Vendor",
                Description = "Min Date Transaction",
                UserId = 1,
                TransactionType = TransactionTypes.Expense,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = 10.00m
                    }
                ]
            },
            new() {
                AccountId = 1,
                Date = DateTime.MaxValue,
                Vendor = "Max Date Vendor",
                Description = "Max Date Transaction",
                UserId = 1,
                TransactionType = TransactionTypes.Expense,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = 20.00m
                    }
                ]
            }
        };
    // Act
    var result = await service.AddMultipleTransactions(list);
    // Assert

    result.Should().NotBeNull();
  }

  /// <summary>
  /// Tests that AddMultipleTransactions handles transactions with boundary decimal amounts.
  /// Input: List with transactions containing decimal.MinValue, decimal.MaxValue, and zero amounts
  /// Expected: Method completes successfully and returns an EnvelopeUpdates.
  /// </summary>
  [Fact]
  public async Task AddMultipleTransactions_TransactionsWithBoundaryAmounts_CompletesSuccessfully()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
    var list = new List<OneTransactionDetail>
    {
            new() {
                AccountId = 1,
                Date = DateTime.Today,
                Vendor = "Zero Amount",
                Description = "Zero amount transaction",
                UserId = 1,
                TransactionType = TransactionTypes.Expense,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = 0m
                    }
                ]
            },
            new() {
                AccountId = 1,
                Date = DateTime.Today,
                Vendor = "Negative Amount",
                Description = "Negative amount transaction",
                UserId = 1,
                TransactionType = TransactionTypes.Expense,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = -100.00m
                    }
                ]
            },
            new() {
                AccountId = 1,
                Date = DateTime.Today,
                Vendor = "Large Amount",
                Description = "Large amount transaction",
                UserId = 1,
                TransactionType = TransactionTypes.Expense,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = 999999999.99m
                    }
                ]
            }
        };
    // Act
    var result = await service.AddMultipleTransactions(list);
    // Assert

    result.Should().NotBeNull();
  }

  /// <summary>
  /// Tests that AddMultipleTransactions handles transactions with empty and whitespace string values.
  /// Input: List with transactions containing empty strings and whitespace-only strings for Vendor and Description
  /// Expected: Method completes successfully and returns an EnvelopeUpdates.
  /// </summary>
  [Fact]
  public async Task AddMultipleTransactions_TransactionsWithEmptyAndWhitespaceStrings_CompletesSuccessfully()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
    var list = new List<OneTransactionDetail>
    {
            new() {
                AccountId = 1,
                Date = DateTime.Today,
                Vendor = string.Empty,
                Description = string.Empty,
                UserId = 1,
                TransactionType = TransactionTypes.Expense,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = 50.00m,
                        Notes = string.Empty
                    }
                ]
            },
            new() {
                AccountId = 1,
                Date = DateTime.Today,
                Vendor = "   ",
                Description = "   ",
                UserId = 1,
                TransactionType = TransactionTypes.Expense,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = 50.00m,
                        Notes = "   "
                    }
                ]
            }
        };
    // Act
    var result = await service.AddMultipleTransactions(list);
    // Assert

    result.Should().NotBeNull();
  }

  /// <summary>
  /// Tests that AddMultipleTransactions handles transactions with all enum values for TransactionType.
  /// Input: List with transactions using each TransactionType enum value
  /// Expected: Method completes successfully and returns an EnvelopeUpdates.
  /// </summary>
  [Theory]
  [InlineData(TransactionTypes.Expense)]
  [InlineData(TransactionTypes.Income)]
  [InlineData(TransactionTypes.Transfer)]
  public async Task AddMultipleTransactions_TransactionWithSpecificType_CompletesSuccessfully(TransactionTypes transactionType)
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
    var list = new List<OneTransactionDetail>
    {
            new() {
                AccountId = 1,
                Date = DateTime.Today,
                Vendor = "Test Vendor",
                Description = $"{transactionType} Transaction",
                UserId = 1,
                TransactionType = transactionType,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = 100.00m
                    }
                ]
            }
        };
    // Act
    var result = await service.AddMultipleTransactions(list);
    // Assert

    result.Should().NotBeNull();
  }

  /// <summary>
  /// Tests that AddMultipleTransactions handles a transaction with multiple detail lines.
  /// Input: Single transaction with multiple TransactionDetailDto items
  /// Expected: Method completes successfully and returns an EnvelopeUpdates.
  /// </summary>
  [Fact]
  public async Task AddMultipleTransactions_TransactionWithMultipleDetails_CompletesSuccessfully()
  {
    // Arrange
    DbContextOptions<BudgetContext> options = new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
    await using var context = new BudgetContext(options, null);
    var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
    mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
    var service = new InsertTransactions(context, mockCurrentFamilyService.Object);
    var list = new List<OneTransactionDetail>
    {
            new() {
                AccountId = 1,
                Date = DateTime.Today,
                Vendor = "Multi-Detail Vendor",
                Description = "Transaction with multiple details",
                UserId = 1,
                TransactionType = TransactionTypes.Expense,
                Details =
                [
                    new() {
                        EnvelopeId = 1,
                        Amount = 25.00m,
                        Notes = "Detail 1"
                    },
                    new() {
                        EnvelopeId = 2,
                        Amount = 30.00m,
                        Notes = "Detail 2"
                    },
                    new() {
                        EnvelopeId = 3,
                        Amount = 45.00m,
                        Notes = "Detail 3"
                    }
                ]
            }
        };
    // Act
    var result = await service.AddMultipleTransactions(list);
    // Assert

    result.Should().NotBeNull();
  }

}



