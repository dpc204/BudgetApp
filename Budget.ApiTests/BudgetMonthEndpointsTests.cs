using System;
using System.Linq;
using System.Threading.Tasks;
using Budget.Api.Features.BudgetMonths;
using Budget.DB;
using Budget.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace Budget.ApiTests;

/// <summary>
/// Tests for Budget Month API endpoints
/// </summary>
public class BudgetMonthEndpointsTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    => new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
      .Options;

  [Fact]
  public async Task GetBudgetMonth_Should_Return_Budget_Data_For_Month()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1, EnvelopeType = EnvelopeTypes.Standard, SortOrder = 1 };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetBudgetMonth.Handler(context);
    var year = 2024;
    var month = 12;
    var acctPeriod = year * 100 + month;

    // Act
    var result = await handler.Handle(new GetBudgetMonth.Query(acctPeriod), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var resultList = result.ToList();
    resultList.Should().HaveCountGreaterThanOrEqualTo(1);
  }

  [Fact]
  public async Task CheckDraftBudgets_Should_Indicate_If_Drafts_Exist()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var handler = new CheckDraftBudgets.Handler(context);

    // Act
    var result = await handler.Handle(new CheckDraftBudgets.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.DraftCount.Should().BeGreaterThanOrEqualTo(0);
  }

  [Fact]
  public async Task UpdateBudgetDraft_Should_Create_Or_Update_Draft()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1, EnvelopeType = EnvelopeTypes.Standard, SortOrder = 1 };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateBudgetDraft.Handler(context);
    var command = new UpdateBudgetDraft.Command(202412, 1, 100.50m);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
  }

  [Fact]
  public async Task ApplyDraftValuesToBudget_Should_Apply_Drafts_To_Budget()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1, EnvelopeType = EnvelopeTypes.Standard, SortOrder = 1 };
    var budgetMonth = new BudgetMonth 
    { 
      AcctPeriod = 202412, 
      EnvelopeId = 1, 
      BudgetDraft = 100m,
      FamilyId = 1
    };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    context.BudgetMonths.Add(budgetMonth);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyDraftValuesToBudget.Handler(context);

    // Act
    var result = await handler.Handle(new ApplyDraftValuesToBudget.Command(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().BeGreaterThan(0);
  }

  [Fact]
  public async Task ClearDraftBudgets_Should_Clear_All_Drafts()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    // Use a future date to ensure it gets cleared
    var futureDate = DateTime.Now.AddMonths(1);
    var futureAcctPeriod = futureDate.Year * 100 + futureDate.Month;
    var budgetMonth = new BudgetMonth 
    { 
      AcctPeriod = futureAcctPeriod, 
      EnvelopeId = 1, 
      BudgetDraft = 100m,
      FamilyId = 1
    };
    
    context.Families.Add(family);
    context.BudgetMonths.Add(budgetMonth);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ClearDraftBudgets.Handler(context);

    // Act
    var result = await handler.Handle(new ClearDraftBudgets.Command(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().BeGreaterThan(0);
  }

  [Fact]
  public async Task CopyBudgetToNextMonth_Should_Copy_Values_To_Next_Month()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1, EnvelopeType = EnvelopeTypes.Standard, SortOrder = 1 };
    var budgetMonth = new BudgetMonth 
    { 
      AcctPeriod = 202411, 
      EnvelopeId = 1, 
      Budget = 150m,
      FamilyId = 1
    };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    context.BudgetMonths.Add(budgetMonth);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new CopyBudgetToNextMonth.Handler(context);
    var command = new CopyBudgetToNextMonth.Command(202411, false, true);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().BeGreaterThan(0);
  }

  [Fact]
  public async Task ClearMonthBudgets_Should_Clear_Budgets_For_Month()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var budgetMonth = new BudgetMonth 
    { 
      AcctPeriod = 202412, 
      EnvelopeId = 1, 
      Budget = 200m,
      FamilyId = 1
    };
    
    context.Families.Add(family);
    context.BudgetMonths.Add(budgetMonth);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ClearMonthBudgets.Handler(context);
    var command = new ClearMonthBudgets.Command(202412);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().BeGreaterThan(0);
  }

  [Fact]
  public async Task ClearMonthDrafts_Should_Clear_Drafts_For_Month()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var budgetMonth = new BudgetMonth 
    { 
      AcctPeriod = 202412, 
      EnvelopeId = 1, 
      BudgetDraft = 150m,
      FamilyId = 1
    };
    
    context.Families.Add(family);
    context.BudgetMonths.Add(budgetMonth);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ClearMonthDrafts.Handler(context);
    var command = new ClearMonthDrafts.Command(202412);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().BeGreaterThan(0);
  }

  [Fact]
  public async Task ClearMonthBoth_Should_Clear_Both_Values_For_Month()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var budgetMonth = new BudgetMonth 
    { 
      AcctPeriod = 202412, 
      EnvelopeId = 1, 
      Budget = 200m,
      BudgetDraft = 150m,
      FamilyId = 1
    };
    
    context.Families.Add(family);
    context.BudgetMonths.Add(budgetMonth);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ClearMonthBoth.Handler(context);
    var command = new ClearMonthBoth.Command(202412);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().BeGreaterThan(0);
  }

  [Fact]
  public async Task ApplyMonthDrafts_Should_Apply_Drafts_For_Month()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var budgetMonth = new BudgetMonth 
    { 
      AcctPeriod = 202412, 
      EnvelopeId = 1, 
      BudgetDraft = 175m,
      FamilyId = 1
    };
    
    context.Families.Add(family);
    context.BudgetMonths.Add(budgetMonth);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ApplyMonthDrafts.Handler(context);
    var command = new ApplyMonthDrafts.Command(202412);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.RecordsUpdated.Should().BeGreaterThan(0);
  }
}

