using System;
using System.Linq;
using System.Threading.Tasks;
using Budget.DB;
using Budget.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace Budget.ApiTests;

/// <summary>
/// Tests to verify multi-tenancy isolation by FamilyId using query filters
/// </summary>
public class MultiTenancyIsolationTests : IntegrationTestBase
{
  // Use a more unique database name to prevent collisions across parallel test runs
  // Also ensure each test gets a completely isolated database by using NewGuid for every call
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions([System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    => new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: $"MultiTenancy_{testName}_{Guid.NewGuid()}")
      .EnableSensitiveDataLogging()
      .EnableServiceProviderCaching(false) // Disable caching to ensure fresh database
      .Options;

  [Fact]
  public async Task Envelopes_Should_Be_Isolated_By_FamilyId()
  {
   // await using var context = new BudgetContext(CreateInMemoryOptions(), familyService);
   
   await using var context = GetTestDBContext(10);
    // Create two families with IDs that don't conflict with seed data
    var family2 = new Family { Id = 20, Name = "Family 20" };
    context.Families.AddRange(family2);
    
    
    // Create categories for both families
    var category1 = new Category { CategoryId = "100", Name = "Cat1", Description = "Cat1", SortOrder = 1, FamilyId = 10, CategoryType = CatTypes.User };
    var category2 = new Category { CategoryId = "101", Name = "Cat2", Description = "Cat2", SortOrder = 1, FamilyId = 20, CategoryType = CatTypes.User };
    context.Categories.AddRange(category1, category2);
    
    // Create envelopes for family 10
    var envelope1Family1 = new Envelope 
    { 
      Id = 500, 
      Name = "Envelope 1 - Family 10", 
      CategoryId = "100", 
      FamilyId = 10,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var envelope2Family1 = new Envelope 
    { 
      Id = 501, 
      Name = "Envelope 2 - Family 10", 
      CategoryId = "100", 
      FamilyId = 10,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 2
    };
    
    // Create envelopes for family 20
    var envelope1Family2 = new Envelope 
    { 
      Id = 502, 
      Name = "Envelope 1 - Family 20", 
      CategoryId = "101", 
      FamilyId = 20,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var envelope2Family2 = new Envelope 
    { 
      Id = 503, 
      Name = "Envelope 2 - Family 20", 
      CategoryId = "101", 
      FamilyId = 20,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 2
    };
    
    context.Envelopes.AddRange(envelope1Family1, envelope2Family1, envelope1Family2, envelope2Family2);
    await context.SaveChangesAsync();
    
    // Clear change tracker to ensure fresh query
    context.ChangeTracker.Clear();

    // Act: Query all envelopes (should only get family 10 due to query filter)
    var envelopes = await context.Envelopes.Where(e => e.Id >= 500).ToListAsync();
    
    // Assert: Should only see family 10 envelopes (query filter should exclude family 20)
    envelopes.Should().HaveCount(2, "query filter should only return Family 10 envelopes");
    envelopes.All(e => e.FamilyId == 10).Should().BeTrue("all envelopes should belong to Family 10");
    envelopes.Any(e => e.Id == 500 || e.Id == 501).Should().BeTrue("should contain Family 10 envelopes");
    envelopes.Any(e => e.Id == 502 || e.Id == 503).Should().BeFalse("should NOT contain Family 20 envelopes");
  }

  [Fact]
  public async Task Transactions_Should_Be_Isolated_By_FamilyId()
  {
    // Arrange
    var familyService = new TestCurrentFamilyService { FamilyId = 10 };
    await using var context = new BudgetContext(CreateInMemoryOptions(), familyService);
    
    // Create two families
    var family1 = new Family { Id = 10, Name = "Family 10" };
    var family2 = new Family { Id = 20, Name = "Family 20" };
    context.Families.AddRange(family1, family2);
    
    // Create accounts for both families
    var account1 = new BankAccount { Id = 200, Name = "Account 1 - Family 10", Balance = 1000m, AccountType = BankAccount.AccountTypes.Checking, FamilyId = 10 };
    var account2 = new BankAccount { Id = 201, Name = "Account 2 - Family 20", Balance = 2000m, AccountType = BankAccount.AccountTypes.Checking, FamilyId = 20 };
    context.BankAccounts.AddRange(account1, account2);
    
    // Create users for both families
    var user1 = new User { Id = 100, Email = "USER1@TEST.COM", FirstName = "User1", LastName = "Family10", FamilyId = 10 };
    var user2 = new User { Id = 101, Email = "USER2@TEST.COM", FirstName = "User2", LastName = "Family20", FamilyId = 20 };
    context.Users.AddRange(user1, user2);
    
    // Create transactions for family 10
    var tx1Family1 = new Transaction 
    { 
      Id = 600, 
      AccountId = 200, 
      Vendor = "Vendor 1", 
      FamilyId = 10,
      Date = DateTime.Now,
      TotalAmount = 100m,
      UserId = 100
    };
    
    // Create transactions for family 20
    var tx1Family2 = new Transaction 
    { 
      Id = 601, 
      AccountId = 201, 
      Vendor = "Vendor 2", 
      FamilyId = 20,
      Date = DateTime.Now,
      TotalAmount = 200m,
      UserId = 101
    };
    
    context.Transactions.AddRange(tx1Family1, tx1Family2);
    await context.SaveChangesAsync();
    
    // Clear change tracker to ensure fresh query
    context.ChangeTracker.Clear();

    // Act
    var transactions = await context.Transactions.Where(t => t.Id >= 600).ToListAsync();
    
    // Assert
    transactions.Should().HaveCount(1, "query filter should only return Family 10 transactions");
    transactions.All(t => t.FamilyId == 10).Should().BeTrue("all transactions should belong to Family 10");
    transactions.First().Id.Should().Be(600, "should only see Family 10 transaction");
  }

  [Fact]
  public async Task BankAccounts_Should_Be_Isolated_By_FamilyId()
  {
    // Arrange
    var familyService = new TestCurrentFamilyService { FamilyId = 10 };
    await using var context = new BudgetContext(CreateInMemoryOptions(), familyService);
    
    // Create two families
    var family1 = new Family { Id = 10, Name = "Family 10" };
    var family2 = new Family { Id = 20, Name = "Family 20" };
    context.Families.AddRange(family1, family2);
    
    // Create accounts
    var account1 = new BankAccount { Id = 300, Name = "Checking - Family 10", Balance = 1000m, AccountType = BankAccount.AccountTypes.Checking, FamilyId = 10 };
    var account2 = new BankAccount { Id = 301, Name = "Savings - Family 20", Balance = 2000m, AccountType = BankAccount.AccountTypes.Checking, FamilyId = 20 };
    context.BankAccounts.AddRange(account1, account2);
    await context.SaveChangesAsync();
    
    // Clear change tracker to ensure fresh query
    context.ChangeTracker.Clear();

    // Act
    var accounts = await context.BankAccounts.Where(a => a.Id >= 300).ToListAsync();
    
    // Assert
    accounts.Should().HaveCount(1, "query filter should only return Family 10 accounts");
    accounts.All(a => a.FamilyId == 10).Should().BeTrue("all accounts should belong to Family 10");
    accounts.First().Id.Should().Be(300, "should only see Family 10 account");
  }

  [Fact]
  public async Task Categories_Should_Be_Isolated_By_FamilyId()
  {
    // Arrange
    var familyService = new TestCurrentFamilyService { FamilyId = 10 };
    await using var context = new BudgetContext(CreateInMemoryOptions(), familyService);
    
    // Create two families
    var family1 = new Family { Id = 10, Name = "Family 10" };
    var family2 = new Family { Id = 20, Name = "Family 20" };
    context.Families.AddRange(family1, family2);
    
    // Create categories
    var cat1 = new Category { CategoryId = "400", Name = "Category 1 - Family 10", Description = "Cat1", SortOrder = 1, FamilyId = 10, CategoryType = CatTypes.User };
    var cat2 = new Category { CategoryId = "401", Name = "Category 2 - Family 20", Description = "Cat2", SortOrder = 1, FamilyId = 20, CategoryType = CatTypes.User };
    context.Categories.AddRange(cat1, cat2);
    await context.SaveChangesAsync();
    
    // Clear change tracker to ensure fresh query
    context.ChangeTracker.Clear();

    // Act
    var categories = await context.Categories.Where(c => int.Parse(c.CategoryId) >= 400).ToListAsync();
    
    // Assert
    categories.Should().HaveCount(1, "query filter should only return Family 10 categories");
    categories.All(c => c.FamilyId == 10).Should().BeTrue("all categories should belong to Family 10");
    categories.First().CategoryId.Should().Be("400", "should only see Family 10 category");
  }

  /// <summary>
  /// Test helper class to provide current family context for multi-tenancy filtering
  /// </summary>
  private class TestCurrentFamilyService : ICurrentFamilyService
  {
    public int FamilyId { get; set; } = 1;
    public int GetCurrentFamilyId() => FamilyId;
  }
}
