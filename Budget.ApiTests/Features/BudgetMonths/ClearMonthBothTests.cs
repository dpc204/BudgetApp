using Budget.Api.Features.BudgetMonths;
using Budget.DB;
using Carter;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Budget.Api.Features.BudgetMonths.UnitTests;


/// <summary>
/// Unit tests for ClearMonthBoth.Handler
/// </summary>
public class ClearMonthBothTests
{
    /// <summary>
    /// Tests that valid AcctPeriod with unlocked records clears both Budget and BudgetDraft values successfully
    /// </summary>
    [Fact]
    public async Task Handle_ValidAcctPeriodWithUnlockedRecords_ClearsValuesAndReturnsSuccess()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var envelope = new Envelope { Id = 1, Name = "Groceries", FamilyId = 1 };
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 1,
            Budget = 100.50m,
            BudgetDraft = 200.75m,
            IsBudgetLocked = false,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Envelopes.Add(envelope);
        context.BudgetMonths.Add(budgetMonth);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(202401);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Cleared budget and draft values for 1 records");
        result.RecordsUpdated.Should().Be(1);

        BudgetMonth? updatedBudget = await context.BudgetMonths.FindAsync(202401, 1);
        updatedBudget.Should().NotBeNull();
        updatedBudget!.Budget.Should().BeNull();
        updatedBudget.BudgetDraft.Should().BeNull();
    }

    /// <summary>
    /// Tests that invalid month values return failure response with appropriate error message
    /// </summary>
    /// <param name="acctPeriod">The accounting period with invalid month component</param>
    /// <param name="description">Description of the test case</param>
    [Theory]
    [InlineData(202400, "month zero")]
    [InlineData(202413, "month thirteen")]
    [InlineData(202499, "month ninety-nine")]
    [InlineData(202300, "month zero in different year")]
    [InlineData(190000, "month zero at boundary year")]
    public async Task Handle_InvalidMonth_ReturnsFailure(int acctPeriod, string description)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(acctPeriod);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid accounting period format");
        result.RecordsUpdated.Should().Be(0);
    }

    /// <summary>
    /// Tests that year values below 1900 return failure response
    /// </summary>
    /// <param name="acctPeriod">The accounting period with invalid year component</param>
    [Theory]
    [InlineData(189912)]
    [InlineData(189901)]
    [InlineData(100001)]
    [InlineData(0)]
    [InlineData(-202401)]
    public async Task Handle_InvalidYear_ReturnsFailure(int acctPeriod)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(acctPeriod);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid accounting period format");
        result.RecordsUpdated.Should().Be(0);
    }

    /// <summary>
    /// Tests that valid AcctPeriod with no matching records returns success with zero count
    /// </summary>
    [Fact]
    public async Task Handle_ValidPeriodNoRecords_ReturnsSuccessWithZeroCount()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(202401);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Cleared budget and draft values for 0 records");
        result.RecordsUpdated.Should().Be(0);
    }

    /// <summary>
    /// Tests that valid AcctPeriod with all locked records returns success with zero count and does not modify records
    /// </summary>
    [Fact]
    public async Task Handle_ValidPeriodAllRecordsLocked_ReturnsSuccessWithZeroCountAndDoesNotModify()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var envelope = new Envelope { Id = 1, Name = "Groceries", FamilyId = 1 };
        var lockedBudget = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 1,
            Budget = 100.50m,
            BudgetDraft = 200.75m,
            IsBudgetLocked = true,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Envelopes.Add(envelope);
        context.BudgetMonths.Add(lockedBudget);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(202401);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecordsUpdated.Should().Be(0);

        BudgetMonth? unchangedBudget = await context.BudgetMonths.FindAsync(202401, 1);
        unchangedBudget.Should().NotBeNull();
        unchangedBudget!.Budget.Should().Be(100.50m);
        unchangedBudget.BudgetDraft.Should().Be(200.75m);
    }

    /// <summary>
    /// Tests that valid AcctPeriod with mix of locked and unlocked records clears only unlocked records
    /// </summary>
    [Fact]
    public async Task Handle_ValidPeriodMixedLockedUnlocked_ClearsOnlyUnlocked()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var envelope1 = new Envelope { Id = 1, Name = "Groceries", FamilyId = 1 };
        var envelope2 = new Envelope { Id = 2, Name = "Gas", FamilyId = 1 };
        var envelope3 = new Envelope { Id = 3, Name = "Entertainment", FamilyId = 1 };

        var unlockedBudget1 = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 1,
            Budget = 100m,
            BudgetDraft = 150m,
            IsBudgetLocked = false,
            FamilyId = 1
        };
        var lockedBudget = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 2,
            Budget = 200m,
            BudgetDraft = 250m,
            IsBudgetLocked = true,
            FamilyId = 1
        };
        var unlockedBudget2 = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 3,
            Budget = 300m,
            BudgetDraft = 350m,
            IsBudgetLocked = false,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Envelopes.AddRange(envelope1, envelope2, envelope3);
        context.BudgetMonths.AddRange(unlockedBudget1, lockedBudget, unlockedBudget2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(202401);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecordsUpdated.Should().Be(2);

        BudgetMonth? clearedBudget1 = await context.BudgetMonths.FindAsync(202401, 1);
        clearedBudget1!.Budget.Should().BeNull();
        clearedBudget1.BudgetDraft.Should().BeNull();

        BudgetMonth? unchangedBudget = await context.BudgetMonths.FindAsync(202401, 2);
        unchangedBudget!.Budget.Should().Be(200m);
        unchangedBudget.BudgetDraft.Should().Be(250m);

        BudgetMonth? clearedBudget2 = await context.BudgetMonths.FindAsync(202401, 3);
        clearedBudget2!.Budget.Should().BeNull();
        clearedBudget2.BudgetDraft.Should().BeNull();
    }

    /// <summary>
    /// Tests that valid AcctPeriod with records already having null values updates successfully
    /// </summary>
    [Fact]
    public async Task Handle_ValidPeriodRecordsAlreadyNull_UpdatesSuccessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var envelope = new Envelope { Id = 1, Name = "Groceries", FamilyId = 1 };
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 1,
            Budget = null,
            BudgetDraft = null,
            IsBudgetLocked = false,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Envelopes.Add(envelope);
        context.BudgetMonths.Add(budgetMonth);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(202401);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecordsUpdated.Should().Be(1);

        BudgetMonth? updatedBudget = await context.BudgetMonths.FindAsync(202401, 1);
        updatedBudget.Should().NotBeNull();
        updatedBudget!.Budget.Should().BeNull();
        updatedBudget.BudgetDraft.Should().BeNull();
    }

    /// <summary>
    /// Tests that boundary month values (1 and 12) are validated correctly
    /// </summary>
    /// <param name="acctPeriod">The accounting period with boundary month values</param>
    [Theory]
    [InlineData(202401)]
    [InlineData(202412)]
    [InlineData(190001)]
    [InlineData(190012)]
    [InlineData(999901)]
    [InlineData(999912)]
    public async Task Handle_BoundaryMonthValues_ValidatesCorrectly(int acctPeriod)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(acctPeriod);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecordsUpdated.Should().Be(0);
    }

    /// <summary>
    /// Tests that boundary year value of 1900 is validated correctly
    /// </summary>
    [Fact]
    public async Task Handle_BoundaryYearValue1900_ValidatesCorrectly()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(190001);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecordsUpdated.Should().Be(0);
    }

    /// <summary>
    /// Tests that multiple records for the same period are all cleared when unlocked
    /// </summary>
    [Fact]
    public async Task Handle_MultipleUnlockedRecordsForSamePeriod_ClearsAllRecords()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var envelope1 = new Envelope { Id = 1, Name = "Groceries", FamilyId = 1 };
        var envelope2 = new Envelope { Id = 2, Name = "Gas", FamilyId = 1 };
        var envelope3 = new Envelope { Id = 3, Name = "Entertainment", FamilyId = 1 };

        var budgets = new List<BudgetMonth>
    {
      new() { AcctPeriod = 202401, EnvelopeId = 1, Budget = 100m, BudgetDraft = 150m, IsBudgetLocked = false, FamilyId = 1 },
      new() { AcctPeriod = 202401, EnvelopeId = 2, Budget = 200m, BudgetDraft = 250m, IsBudgetLocked = false, FamilyId = 1 },
      new() { AcctPeriod = 202401, EnvelopeId = 3, Budget = 300m, BudgetDraft = 350m, IsBudgetLocked = false, FamilyId = 1 }
    };

        context.Families.Add(family);
        context.Envelopes.AddRange(envelope1, envelope2, envelope3);
        context.BudgetMonths.AddRange(budgets);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(202401);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecordsUpdated.Should().Be(3);
        result.Message.Should().Contain("Cleared budget and draft values for 3 records");

        List<BudgetMonth> allBudgets = await context.BudgetMonths.Where(b => b.AcctPeriod == 202401).ToListAsync();
        allBudgets.Should().HaveCount(3);
        allBudgets.Should().OnlyContain(b => b.Budget == null && b.BudgetDraft == null);
    }

    /// <summary>
    /// Tests that records from different periods are not affected
    /// </summary>
    [Fact]
    public async Task Handle_RecordsFromDifferentPeriods_OnlyTargetPeriodCleared()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var envelope = new Envelope { Id = 1, Name = "Groceries", FamilyId = 1 };

        var targetBudget = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 1,
            Budget = 100m,
            BudgetDraft = 150m,
            IsBudgetLocked = false,
            FamilyId = 1
        };
        var otherBudget = new BudgetMonth
        {
            AcctPeriod = 202402,
            EnvelopeId = 1,
            Budget = 200m,
            BudgetDraft = 250m,
            IsBudgetLocked = false,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Envelopes.Add(envelope);
        context.BudgetMonths.AddRange(targetBudget, otherBudget);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(202401);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecordsUpdated.Should().Be(1);

        BudgetMonth? clearedBudget = await context.BudgetMonths.FindAsync(202401, 1);
        clearedBudget!.Budget.Should().BeNull();
        clearedBudget.BudgetDraft.Should().BeNull();

        BudgetMonth? unchangedBudget = await context.BudgetMonths.FindAsync(202402, 1);
        unchangedBudget!.Budget.Should().Be(200m);
        unchangedBudget.BudgetDraft.Should().Be(250m);
    }

    /// <summary>
    /// Tests extreme valid AcctPeriod values at integer boundaries
    /// </summary>
    [Theory]
    [InlineData(int.MaxValue)]
    public async Task Handle_ExtremeValidAcctPeriod_HandlesCorrectly(int acctPeriod)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(acctPeriod);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        // int.MaxValue / 100 = 21474836, month = 47, so should fail validation
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid accounting period format");
    }

    /// <summary>
    /// Tests extreme invalid AcctPeriod values at negative boundary
    /// </summary>
    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    public async Task Handle_ExtremeInvalidAcctPeriod_ReturnsFailure(int acctPeriod)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new ClearMonthBoth.Handler(context);
        var command = new ClearMonthBoth.Command(acctPeriod);

        // Act
        ClearMonthBoth.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid accounting period format");
        result.RecordsUpdated.Should().Be(0);
    }

    /// <summary>
    /// Creates in-memory database options for testing
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .Options;
    }
}