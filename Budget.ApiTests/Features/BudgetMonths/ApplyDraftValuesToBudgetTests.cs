using Budget.Api.Features.BudgetMonths;

namespace Budget.ApiTests.Features.BudgetMonths;


/// <summary>
/// Unit tests for ApplyDraftValuesToBudget.Handler
/// </summary>
public class ApplyDraftValuesToBudgetTests
{
  /// <summary>
  /// Creates an in-memory database options for testing
  /// </summary>
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;
  }

  /// <summary>
  /// Tests that Handle applies draft values to unlocked budgets and returns correct count
  /// Input: Multiple budgets with drafts, all unlocked
  /// Expected: All draft values are applied, drafts are cleared, success response with correct count
  /// </summary>
  [Fact]
  public async Task Handle_WithUnlockedBudgetsWithDrafts_AppliesDraftsAndClearsThem()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var envelope1 = new Envelope { Id = 1, Name = "Envelope 1", FamilyId = 1 };
    var envelope2 = new Envelope { Id = 2, Name = "Envelope 2", FamilyId = 1 };

    context.Families.Add(family);
    context.Envelopes.AddRange(envelope1, envelope2);
    context.BudgetMonths.AddRange([
      new BudgetMonth
          {
              AcctPeriod = 202401,
              EnvelopeId = 1,
              Budget = 100.00m,
              BudgetDraft = 150.00m,
              IsBudgetLocked = false,
              FamilyId = 1
          },
          new BudgetMonth
          {
              AcctPeriod = 202401,
              EnvelopeId = 2,
              Budget = 200.00m,
              BudgetDraft = 250.00m,
              IsBudgetLocked = false,
              FamilyId = 1
          }
    ]);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(2);
    result.Message.Should().Be("Applied draft values to 2 budget records");

    BudgetMonth? updatedBudget1 = await context.BudgetMonths.FindAsync([202401, 1], TestContext.Current.CancellationToken);
    updatedBudget1.Should().NotBeNull();
    updatedBudget1!.Budget.Should().Be(150.00m);
    updatedBudget1.BudgetDraft.Should().BeNull();

    BudgetMonth? updatedBudget2 = await context.BudgetMonths.FindAsync([202401, 2], TestContext.Current.CancellationToken);
    updatedBudget2.Should().NotBeNull();
    updatedBudget2!.Budget.Should().Be(250.00m);
    updatedBudget2.BudgetDraft.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle skips locked budgets when applying drafts
  /// Input: Budgets with drafts, some locked and some unlocked
  /// Expected: Only unlocked budgets are updated, locked budgets remain unchanged, count includes all with drafts
  /// </summary>
  [Fact]
  public async Task Handle_WithLockedBudgetsWithDrafts_SkipsLockedBudgets()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var envelope1 = new Envelope { Id = 1, Name = "Envelope 1", FamilyId = 1 };
    var envelope2 = new Envelope { Id = 2, Name = "Envelope 2", FamilyId = 1 };

    context.Families.Add(family);
    context.Envelopes.AddRange([envelope1, envelope2]);
    context.BudgetMonths.AddRange([
      new BudgetMonth
          {
              AcctPeriod = 202401,
              EnvelopeId = 1,
              Budget = 100.00m,
              BudgetDraft = 150.00m,
              IsBudgetLocked = false,
              FamilyId = 1
          },
          new BudgetMonth
          {
              AcctPeriod = 202401,
              EnvelopeId = 2,
              Budget = 200.00m,
              BudgetDraft = 250.00m,
              IsBudgetLocked = true,
              FamilyId = 1
          }
    ]);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(2);
    result.Message.Should().Be("Applied draft values to 2 budget records");

    BudgetMonth? unlockedBudget = await context.BudgetMonths.FindAsync([202401, 1], TestContext.Current.CancellationToken);
    unlockedBudget.Should().NotBeNull();
    unlockedBudget!.Budget.Should().Be(150.00m);
    unlockedBudget.BudgetDraft.Should().BeNull();

    BudgetMonth? lockedBudget = await context.BudgetMonths.FindAsync([202401, 2], TestContext.Current.CancellationToken);
    lockedBudget.Should().NotBeNull();
    lockedBudget!.Budget.Should().Be(200.00m);
    lockedBudget.BudgetDraft.Should().Be(250.00m);
  }

  /// <summary>
  /// Tests that Handle returns zero count when no budgets have drafts
  /// Input: Budgets without draft values
  /// Expected: No updates occur, success response with zero count
  /// </summary>
  [Fact]
  public async Task Handle_WithNoBudgetsWithDrafts_ReturnsZeroCount()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var envelope = new Envelope { Id = 1, Name = "Envelope 1", FamilyId = 1 };

    context.Families.Add(family);
    context.Envelopes.Add(envelope);
    context.BudgetMonths.Add(
      new BudgetMonth {
        AcctPeriod = 202401,
        EnvelopeId = 1,
        Budget = 100.00m,
        BudgetDraft = null,
        IsBudgetLocked = false,
        FamilyId = 1
      }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(0);
    result.Message.Should().Be("Applied draft values to 0 budget records");
  }

  /// <summary>
  /// Tests that Handle works correctly with empty database
  /// Input: No budget records in database
  /// Expected: Success response with zero count
  /// </summary>
  [Fact]
  public async Task Handle_WithEmptyDatabase_ReturnsZeroCount()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(0);
    result.Message.Should().Be("Applied draft values to 0 budget records");
  }

  /// <summary>
  /// Tests that Handle does not update locked budgets even when all have drafts
  /// Input: All budgets with drafts are locked
  /// Expected: No budgets are updated, drafts remain, count includes all budgets with drafts
  /// </summary>
  [Fact]
  public async Task Handle_WithAllLockedBudgetsWithDrafts_DoesNotUpdateAny()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var envelope = new Envelope { Id = 1, Name = "Envelope 1", FamilyId = 1 };

    context.Families.Add(family);
    context.Envelopes.Add(envelope);
    context.BudgetMonths.Add(
      new BudgetMonth {
        AcctPeriod = 202401,
        EnvelopeId = 1,
        Budget = 100.00m,
        BudgetDraft = 150.00m,
        IsBudgetLocked = true,
        FamilyId = 1
      }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(1);
    result.Message.Should().Be("Applied draft values to 1 budget records");

    BudgetMonth? lockedBudget = await context.BudgetMonths.FindAsync([202401, 1], TestContext.Current.CancellationToken);
    lockedBudget.Should().NotBeNull();
    lockedBudget!.Budget.Should().Be(100.00m);
    lockedBudget.BudgetDraft.Should().Be(150.00m);
  }

  /// <summary>
  /// Tests that Handle applies null budget draft values correctly
  /// Input: Budget with null Budget value and non-null BudgetDraft
  /// Expected: Budget is set to BudgetDraft value, BudgetDraft is cleared
  /// </summary>
  [Fact]
  public async Task Handle_WithNullBudgetValue_AppliesDraftCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var envelope = new Envelope { Id = 1, Name = "Envelope 1", FamilyId = 1 };

    context.Families.Add(family);
    context.Envelopes.Add(envelope);
    context.BudgetMonths.Add(
      new BudgetMonth {
        AcctPeriod = 202401,
        EnvelopeId = 1,
        Budget = null,
        BudgetDraft = 150.00m,
        IsBudgetLocked = false,
        FamilyId = 1
      }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(1);

    BudgetMonth? updatedBudget = await context.BudgetMonths.FindAsync([202401, 1], TestContext.Current.CancellationToken);
    updatedBudget.Should().NotBeNull();
    updatedBudget!.Budget.Should().Be(150.00m);
    updatedBudget.BudgetDraft.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle applies zero value drafts correctly
  /// Input: Budget with zero BudgetDraft value
  /// Expected: Budget is set to zero, BudgetDraft is cleared
  /// </summary>
  [Fact]
  public async Task Handle_WithZeroDraftValue_AppliesZeroCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var envelope = new Envelope { Id = 1, Name = "Envelope 1", FamilyId = 1 };

    context.Families.Add(family);
    context.Envelopes.Add(envelope);
    context.BudgetMonths.Add(
      new BudgetMonth {
        AcctPeriod = 202401,
        EnvelopeId = 1,
        Budget = 100.00m,
        BudgetDraft = 0.00m,
        IsBudgetLocked = false,
        FamilyId = 1
      }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(1);

    BudgetMonth? updatedBudget = await context.BudgetMonths.FindAsync([202401, 1], TestContext.Current.CancellationToken);
    updatedBudget.Should().NotBeNull();
    updatedBudget!.Budget.Should().Be(0.00m);
    updatedBudget.BudgetDraft.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle applies negative draft values correctly
  /// Input: Budget with negative BudgetDraft value
  /// Expected: Budget is set to negative value, BudgetDraft is cleared
  /// </summary>
  [Fact]
  public async Task Handle_WithNegativeDraftValue_AppliesNegativeCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var envelope = new Envelope { Id = 1, Name = "Envelope 1", FamilyId = 1 };

    context.Families.Add(family);
    context.Envelopes.Add(envelope);
    context.BudgetMonths.Add(
      new BudgetMonth {
        AcctPeriod = 202401,
        EnvelopeId = 1,
        Budget = 100.00m,
        BudgetDraft = -50.00m,
        IsBudgetLocked = false,
        FamilyId = 1
      }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(1);

    BudgetMonth? updatedBudget = await context.BudgetMonths.FindAsync([202401, 1], TestContext.Current.CancellationToken);
    updatedBudget.Should().NotBeNull();
    updatedBudget!.Budget.Should().Be(-50.00m);
    updatedBudget.BudgetDraft.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle applies very large draft values correctly
  /// Input: Budget with maximum decimal BudgetDraft value
  /// Expected: Budget is set to large value, BudgetDraft is cleared
  /// </summary>
  [Fact]
  public async Task Handle_WithVeryLargeDraftValue_AppliesLargeValueCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var envelope = new Envelope { Id = 1, Name = "Envelope 1", FamilyId = 1 };

    context.Families.Add(family);
    context.Envelopes.Add(envelope);
    context.BudgetMonths.Add(
      new BudgetMonth {
        AcctPeriod = 202401,
        EnvelopeId = 1,
        Budget = 100.00m,
        BudgetDraft = 999999999999.99m,
        IsBudgetLocked = false,
        FamilyId = 1
      }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(1);

    BudgetMonth? updatedBudget = await context.BudgetMonths.FindAsync([202401, 1], TestContext.Current.CancellationToken);
    updatedBudget.Should().NotBeNull();
    updatedBudget!.Budget.Should().Be(999999999999.99m);
    updatedBudget.BudgetDraft.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle processes multiple budgets across different periods
  /// Input: Multiple budgets with drafts for different account periods
  /// Expected: All unlocked budgets are updated regardless of period
  /// </summary>
  [Fact]
  public async Task Handle_WithMultiplePeriodsWithDrafts_AppliesAllDrafts()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var envelope = new Envelope { Id = 1, Name = "Envelope 1", FamilyId = 1 };

    context.Families.Add(family);
    context.Envelopes.Add(envelope);
    context.BudgetMonths.AddRange(
      new BudgetMonth {
        AcctPeriod = 202401,
        EnvelopeId = 1,
        Budget = 100.00m,
        BudgetDraft = 150.00m,
        IsBudgetLocked = false,
        FamilyId = 1
      },
      new BudgetMonth {
        AcctPeriod = 202402,
        EnvelopeId = 1,
        Budget = 200.00m,
        BudgetDraft = 250.00m,
        IsBudgetLocked = false,
        FamilyId = 1
      }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(2);

    BudgetMonth? updatedBudget1 = await context.BudgetMonths.FindAsync([202401, 1], TestContext.Current.CancellationToken);
    updatedBudget1.Should().NotBeNull();
    updatedBudget1!.Budget.Should().Be(150.00m);
    updatedBudget1.BudgetDraft.Should().BeNull();

    BudgetMonth? updatedBudget2 = await context.BudgetMonths.FindAsync([202402, 1], TestContext.Current.CancellationToken);
    updatedBudget2.Should().NotBeNull();
    updatedBudget2!.Budget.Should().Be(250.00m);
    updatedBudget2.BudgetDraft.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle respects cancellation token during query execution
  /// Input: Cancelled cancellation token
  /// Expected: OperationCanceledException is thrown
  /// </summary>
  [Fact]
  public async Task Handle_WithCancelledToken_ThrowsOperationCanceledException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();
    var cts = new CancellationTokenSource();
    cts.Cancel();

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(
      async () => await handler.Handle(command, cts.Token)
    );
  }

  /// <summary>
  /// Tests that Handle processes budgets with mix of null and non-null Budget values
  /// Input: Multiple budgets, some with null Budget, all with drafts
  /// Expected: All unlocked budgets get their drafts applied regardless of initial Budget value
  /// </summary>
  [Fact]
  public async Task Handle_WithMixedNullAndNonNullBudgets_AppliesAllDrafts()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var envelope1 = new Envelope { Id = 1, Name = "Envelope 1", FamilyId = 1 };
    var envelope2 = new Envelope { Id = 2, Name = "Envelope 2", FamilyId = 1 };

    context.Families.Add(family);
    context.Envelopes.AddRange(envelope1, envelope2);
    context.BudgetMonths.AddRange(
      new BudgetMonth {
        AcctPeriod = 202401,
        EnvelopeId = 1,
        Budget = null,
        BudgetDraft = 150.00m,
        IsBudgetLocked = false,
        FamilyId = 1
      },
      new BudgetMonth {
        AcctPeriod = 202401,
        EnvelopeId = 2,
        Budget = 200.00m,
        BudgetDraft = 250.00m,
        IsBudgetLocked = false,
        FamilyId = 1
      }
    );
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);
    var command = new ApplyDraftValuesToBudget.Command();

    // Act
    ApplyDraftValuesToBudget.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().Be(2);

    BudgetMonth? updatedBudget1 = await context.BudgetMonths.FindAsync([202401, 1], TestContext.Current.CancellationToken);
    updatedBudget1.Should().NotBeNull();
    updatedBudget1!.Budget.Should().Be(150.00m);
    updatedBudget1.BudgetDraft.Should().BeNull();

    BudgetMonth? updatedBudget2 = await context.BudgetMonths.FindAsync([202401, 2], TestContext.Current.CancellationToken);
    updatedBudget2.Should().NotBeNull();
    updatedBudget2!.Budget.Should().Be(250.00m);
    updatedBudget2.BudgetDraft.Should().BeNull();
  }
}