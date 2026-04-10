namespace Budget.ApiTests.Features.Transactions;


/// <summary>
/// Unit tests for the MoveEnvelopeBalance class.
/// </summary>
public partial class MoveEnvelopeBalanceTests : IntegrationTestBase
{
  /// <summary>
  /// Creates in-memory database options for testing.
  /// </summary>
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
  }

  /// <summary>
  /// Tests that MoveBalance throws InvalidOperationException when both envelopes do not exist.
  /// Input: Non-existent fromEnvelopeId and toEnvelopeId.
  /// Expected: InvalidOperationException with message "One or both envelopes do not exist."
  /// </summary>
  [Fact]
  public async Task MoveBalance_BothEnvelopesNotFound_ThrowsInvalidOperationException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    context.Families.Add(family);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var userAndOpts = MakeUserAndOptions();


    var service = new MoveEnvelopeBalance(userAndOpts);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        await service.MoveBalance(context, 999, 888, 100m));

    exception.Message.Should().Be("One or both envelopes do not exist.");
  }

  /// <summary>
  /// Tests that MoveBalance successfully transfers a positive amount between two valid envelopes.
  /// Input: fromEnvelopeId=1, toEnvelopeId=2, amountToMove=100
  /// Expected: fromEnvelope balance decreased by 100, toEnvelope balance increased by 100
  /// </summary>
  [Fact]
  public async Task MoveBalance_ValidEnvelopesAndPositiveAmount_TransfersBalanceSuccessfully()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var fromEnvelope = new Envelope { Id = 1, Balance = 500m, FamilyId = 1, Name = "From" };
    var toEnvelope = new Envelope { Id = 2, Balance = 200m, FamilyId = 1, Name = "To" };
    context.Envelopes.AddRange(fromEnvelope, toEnvelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var service = new MoveEnvelopeBalance(MakeUserAndOptions());

    // Act
    await service.MoveBalance(context, fromEnvelopeId: 1, toEnvelopeId: 2, amountToMove: 100m);

    // Assert
    var updatedFrom = await context.Envelopes.FindAsync([1], TestContext.Current.CancellationToken);
    var updatedTo = await context.Envelopes.FindAsync([2], TestContext.Current.CancellationToken);
    Assert.NotNull(updatedFrom);
    Assert.NotNull(updatedTo);
    Assert.Equal(400m, updatedFrom.Balance);
    Assert.Equal(300m, updatedTo.Balance);
  }

  /// <summary>
  /// Tests that MoveBalance throws InvalidOperationException when fromEnvelopeId does not exist.
  /// Input: fromEnvelopeId=999 (non-existent), toEnvelopeId=2 (valid), amountToMove=100
  /// Expected: InvalidOperationException with message "One or both envelopes do not exist."
  /// </summary>
  [Fact]
  public async Task MoveBalance_FromEnvelopeDoesNotExist_ThrowsInvalidOperationException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var toEnvelope = new Envelope { Id = 2, Balance = 200m, FamilyId = 1, Name = "To" };
    context.Envelopes.Add(toEnvelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var service = new MoveEnvelopeBalance(MakeUserAndOptions());

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        await service.MoveBalance(context, fromEnvelopeId: 999, toEnvelopeId: 2, amountToMove: 100m));
    Assert.Equal("One or both envelopes do not exist.", exception.Message);
  }

  /// <summary>
  /// Tests that MoveBalance throws InvalidOperationException when toEnvelopeId does not exist.
  /// Input: fromEnvelopeId=1 (valid), toEnvelopeId=999 (non-existent), amountToMove=100
  /// Expected: InvalidOperationException with message "One or both envelopes do not exist."
  /// </summary>
  [Fact]
  public async Task MoveBalance_ToEnvelopeDoesNotExist_ThrowsInvalidOperationException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var fromEnvelope = new Envelope { Id = 1, Balance = 500m, FamilyId = 1, Name = "From" };
    context.Envelopes.Add(fromEnvelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var service = new MoveEnvelopeBalance(MakeUserAndOptions());

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        await service.MoveBalance(context, fromEnvelopeId: 1, toEnvelopeId: 999, amountToMove: 100m));
    Assert.Equal("One or both envelopes do not exist.", exception.Message);
  }

  /// <summary>
  /// Tests that MoveBalance throws InvalidOperationException when both envelopes do not exist.
  /// Input: fromEnvelopeId=998 (non-existent), toEnvelopeId=999 (non-existent), amountToMove=100
  /// Expected: InvalidOperationException with message "One or both envelopes do not exist."
  /// </summary>
  [Fact]
  public async Task MoveBalance_BothEnvelopesDoNotExist_ThrowsInvalidOperationException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var service = new MoveEnvelopeBalance(MakeUserAndOptions());

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        await service.MoveBalance(context, fromEnvelopeId: 998, toEnvelopeId: 999, amountToMove: 100m));
    Assert.Equal("One or both envelopes do not exist.", exception.Message);
  }

  /// <summary>
  /// Tests that MoveBalance handles zero amount correctly without changing balances.
  /// Input: fromEnvelopeId=1, toEnvelopeId=2, amountToMove=0
  /// Expected: Both envelope balances remain unchanged
  /// </summary>
  [Fact]
  public async Task MoveBalance_ZeroAmount_LeavesBalancesUnchanged()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var fromEnvelope = new Envelope { Id = 1, Balance = 500m, FamilyId = 1, Name = "From" };
    var toEnvelope = new Envelope { Id = 2, Balance = 200m, FamilyId = 1, Name = "To" };
    context.Envelopes.AddRange(fromEnvelope, toEnvelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var service = new MoveEnvelopeBalance(MakeUserAndOptions());

    // Act
    await service.MoveBalance(context, fromEnvelopeId: 1, toEnvelopeId: 2, amountToMove: 0m);

    // Assert
    var updatedFrom = await context.Envelopes.FindAsync([1], TestContext.Current.CancellationToken);
    var updatedTo = await context.Envelopes.FindAsync([2], TestContext.Current.CancellationToken);
    Assert.NotNull(updatedFrom);
    Assert.NotNull(updatedTo);
    Assert.Equal(500m, updatedFrom.Balance);
    Assert.Equal(200m, updatedTo.Balance);
  }

  /// <summary>
  /// Tests that MoveBalance allows transfers that result in negative balance since the balance check is commented out.
  /// Input: fromEnvelopeId=1 with balance 50, toEnvelopeId=2, amountToMove=100
  /// Expected: fromEnvelope balance becomes -50 (allows negative balance)
  /// </summary>
  [Fact]
  [Trait("Category", "ProductionBugSuspected")]
  public async Task MoveBalance_InsufficientBalance_AllowsNegativeBalance()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var fromEnvelope = new Envelope { Id = 1, Balance = 50m, FamilyId = 1, Name = "From" };
    var toEnvelope = new Envelope { Id = 2, Balance = 200m, FamilyId = 1, Name = "To" };
    context.Envelopes.AddRange(fromEnvelope, toEnvelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var service = new MoveEnvelopeBalance(MakeUserAndOptions());

    // Act
    await service.MoveBalance(context, fromEnvelopeId: 1, toEnvelopeId: 2, amountToMove: 100m);

    // Assert
    var updatedFrom = await context.Envelopes.FindAsync([1], TestContext.Current.CancellationToken);
    var updatedTo = await context.Envelopes.FindAsync([2], TestContext.Current.CancellationToken);
    Assert.NotNull(updatedFrom);
    Assert.NotNull(updatedTo);
    Assert.Equal(-50m, updatedFrom.Balance);
    Assert.Equal(300m, updatedTo.Balance);
  }

  /// <summary>
  /// Tests that MoveBalance handles the same envelope ID for both source and destination.
  /// Input: fromEnvelopeId=1, toEnvelopeId=1, amountToMove=100
  /// Expected: Balance remains unchanged (adds and subtracts same amount)
  /// </summary>
  [Fact]
  public async Task MoveBalance_SameEnvelopeIds_LeavesBalanceUnchanged()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var envelope = new Envelope { Id = 1, Balance = 500m, FamilyId = 1, Name = "Same" };
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var service = new MoveEnvelopeBalance(MakeUserAndOptions());

    // Act
    await service.MoveBalance(context, fromEnvelopeId: 1, toEnvelopeId: 1, amountToMove: 100m);

    // Assert
    var updated = await context.Envelopes.FindAsync([1], TestContext.Current.CancellationToken);
    Assert.NotNull(updated);
    Assert.Equal(500m, updated.Balance);
  }

  /// <summary>
  /// Tests that MoveBalance handles extreme decimal amounts correctly.
  /// Input: Very large, very small, and extreme decimal values for amountToMove
  /// Expected: Balances updated correctly without overflow or precision loss
  /// </summary>
  [Theory]
  [InlineData(0.01)]
  [InlineData(0.0001)]
  [InlineData(1000000000)]
  [InlineData(-1000000000)]
  public async Task MoveBalance_ExtremeAmounts_HandlesCorrectly(decimal amountToMove)
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var fromEnvelope = new Envelope { Id = 1, Balance = 2000000000m, FamilyId = 1, Name = "From" };
    var toEnvelope = new Envelope { Id = 2, Balance = 2000000000m, FamilyId = 1, Name = "To" };
    context.Envelopes.AddRange(fromEnvelope, toEnvelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var service = new MoveEnvelopeBalance(MakeUserAndOptions());

    // Act
    await service.MoveBalance(context, fromEnvelopeId: 1, toEnvelopeId: 2, amountToMove);

    // Assert
    var updatedFrom = await context.Envelopes.FindAsync([1], TestContext.Current.CancellationToken);
    var updatedTo = await context.Envelopes.FindAsync([2], TestContext.Current.CancellationToken);
    Assert.NotNull(updatedFrom);
    Assert.NotNull(updatedTo);
    Assert.Equal(2000000000m - amountToMove, updatedFrom.Balance);
    Assert.Equal(2000000000m + amountToMove, updatedTo.Balance);
  }

  /// <summary>
  /// Tests that MoveBalance persists changes to the database by verifying SaveChangesAsync is called.
  /// Input: Valid envelopes and amount
  /// Expected: Changes are saved and retrievable from a new context instance
  /// </summary>
  [Fact]
  public async Task MoveBalance_ValidTransfer_PersistsChangesToDatabase()
  {
    // Arrange
    var options = CreateInMemoryOptions();
    await using(var context = new BudgetContext(options, null))
    {
      var fromEnvelope = new Envelope { Id = 1, Balance = 500m, FamilyId = 1, Name = "From" };
      var toEnvelope = new Envelope { Id = 2, Balance = 200m, FamilyId = 1, Name = "To" };
      context.Envelopes.AddRange(fromEnvelope, toEnvelope);
      await context.SaveChangesAsync(TestContext.Current.CancellationToken);

      var service = new MoveEnvelopeBalance(MakeUserAndOptions());

      // Act
      await service.MoveBalance(context, fromEnvelopeId: 1, toEnvelopeId: 2, amountToMove: 100m);
    }

    // Assert - verify changes persist in a new context
    await using var verifyContext = new BudgetContext(options, null);
    var updatedFrom = await verifyContext.Envelopes.FindAsync([1], TestContext.Current.CancellationToken);
    var updatedTo = await verifyContext.Envelopes.FindAsync([2], TestContext.Current.CancellationToken);
    Assert.NotNull(updatedFrom);
    Assert.NotNull(updatedTo);
    Assert.Equal(400m, updatedFrom.Balance);
    Assert.Equal(300m, updatedTo.Balance);
  }

  /// <summary>
  /// Tests that MoveBalance correctly handles decimal precision with very small fractional amounts.
  /// Input: fromEnvelopeId=1, toEnvelopeId=2, amountToMove=0.123456789m
  /// Expected: Precise decimal values are maintained in both envelopes
  /// </summary>
  [Fact]
  public async Task MoveBalance_SmallFractionalAmount_MaintainsDecimalPrecision()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var fromEnvelope = new Envelope { Id = 1, Balance = 100.123456789m, FamilyId = 1, Name = "From" };
    var toEnvelope = new Envelope { Id = 2, Balance = 50.987654321m, FamilyId = 1, Name = "To" };
    context.Envelopes.AddRange(fromEnvelope, toEnvelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var service = new MoveEnvelopeBalance(MakeUserAndOptions());

    // Act
    await service.MoveBalance(context, fromEnvelopeId: 1, toEnvelopeId: 2, amountToMove: 0.123456789m);

    // Assert
    var updatedFrom = await context.Envelopes.FindAsync([1], TestContext.Current.CancellationToken);
    var updatedTo = await context.Envelopes.FindAsync([2], TestContext.Current.CancellationToken);
    Assert.NotNull(updatedFrom);
    Assert.NotNull(updatedTo);
    Assert.Equal(100m, updatedFrom.Balance);
    Assert.Equal(51.11111111m, updatedTo.Balance);
  }

  /// <summary>
  /// Tests that MoveBalance handles multiple consecutive transfers correctly.
  /// Input: Three consecutive transfers between envelopes
  /// Expected: All balances are correctly accumulated
  /// </summary>
  [Fact]
  public async Task MoveBalance_MultipleConsecutiveTransfers_AccumulatesBalancesCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var envelope1 = new Envelope { Id = 1, Balance = 1000m, FamilyId = 1, Name = "Envelope1" };
    var envelope2 = new Envelope { Id = 2, Balance = 500m, FamilyId = 1, Name = "Envelope2" };
    var envelope3 = new Envelope { Id = 3, Balance = 250m, FamilyId = 1, Name = "Envelope3" };
    context.Envelopes.AddRange(envelope1, envelope2, envelope3);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var service = new MoveEnvelopeBalance(MakeUserAndOptions());

    // Act
    await service.MoveBalance(context, fromEnvelopeId: 1, toEnvelopeId: 2, amountToMove: 100m);
    await service.MoveBalance(context, fromEnvelopeId: 2, toEnvelopeId: 3, amountToMove: 50m);
    await service.MoveBalance(context, fromEnvelopeId: 3, toEnvelopeId: 1, amountToMove: 25m);

    // Assert
    var updated1 = await context.Envelopes.FindAsync([1], TestContext.Current.CancellationToken);
    var updated2 = await context.Envelopes.FindAsync([2], TestContext.Current.CancellationToken);
    var updated3 = await context.Envelopes.FindAsync([3], TestContext.Current.CancellationToken);
    Assert.NotNull(updated1);
    Assert.NotNull(updated2);
    Assert.NotNull(updated3);
    Assert.Equal(925m, updated1.Balance);
    Assert.Equal(550m, updated2.Balance);
    Assert.Equal(275m, updated3.Balance);
  }
}
