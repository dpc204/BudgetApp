using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Budget.Api.Features.Envelopes;
using Budget.DB;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Budget.ApiTests;

/// <summary>
/// Tests to verify multi-tenancy isolation by FamilyId
/// </summary>
public class MultiTenancyIsolationTests : IntegrationTestBase
{
  /// <summary>
  /// Test that envelopes from different families are isolated
  /// </summary>
  [Fact]
  public async Task Envelopes_Should_Be_Isolated_By_FamilyId()
  {
    // Arrange: Create two families and envelopes for each
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      
      // Create second family
      var family2 = new Family { Id = 2, Name = "Family 2" };
      db.Families.Add(family2);
      
      // Create category for both families
      var category1 = TestHelpers.CreateTestCategory(id: "100", name: "Cat1", familyId: 1);
      var category2 = TestHelpers.CreateTestCategory(id: "101", name: "Cat2", familyId: 2);
      db.Categories.Add(category1);
      db.Categories.Add(category2);
      
      // Create envelopes for family 1
      var envelope1Family1 = TestHelpers.CreateTestEnvelope(id: 500, name: "Envelope 1 - Family 1", categoryId: "100", familyId: 1);
      var envelope2Family1 = TestHelpers.CreateTestEnvelope(id: 501, name: "Envelope 2 - Family 1", categoryId: "100", familyId: 1);
      
      // Create envelopes for family 2
      var envelope1Family2 = TestHelpers.CreateTestEnvelope(id: 502, name: "Envelope 1 - Family 2", categoryId: "101", familyId: 2);
      var envelope2Family2 = TestHelpers.CreateTestEnvelope(id: 503, name: "Envelope 2 - Family 2", categoryId: "101", familyId: 2);
      
      db.Envelopes.AddRange(envelope1Family1, envelope2Family1, envelope1Family2, envelope2Family2);
      await db.SaveChangesAsync();
    }

    // Act: Query all envelopes (should only get family 1 due to query filter)
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var envelopes = await db.Envelopes.Where(e => e.Id >= 500).ToListAsync();
      
      // Assert: Should only see family 1 envelopes (filtering out seed data)
      envelopes.Should().HaveCount(2, "query filter should only return Family 1 envelopes");
      envelopes.All(e => e.FamilyId == 1).Should().BeTrue("all envelopes should belong to Family 1");
      envelopes.Any(e => e.Id == 500 || e.Id == 501).Should().BeTrue("should contain Family 1 envelopes");
      envelopes.Any(e => e.Id == 502 || e.Id == 503).Should().BeFalse("should NOT contain Family 2 envelopes");
    }
  }

  /// <summary>
  /// Test that transactions from different families are isolated
  /// </summary>
  [Fact]
  public async Task Transactions_Should_Be_Isolated_By_FamilyId()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      
      // Create second family
      var family2 = new Family { Id = 2, Name = "Family 2" };
      db.Families.Add(family2);
      
      // Create accounts for both families
      var account1 = TestHelpers.CreateTestAccount(id: 200, name: "Account 1 - Family 1", familyId: 1);
      var account2 = TestHelpers.CreateTestAccount(id: 201, name: "Account 2 - Family 2", familyId: 2);
      db.BankAccounts.AddRange(account1, account2);
      
      // Create users for both families
      var user1 = new User { Id = 100, FirstName = "User1", LastName = "Family1", FamilyId = 1 };
      var user2 = new User { Id = 101, FirstName = "User2", LastName = "Family2", FamilyId = 2 };
      db.Users.AddRange(user1, user2);
      
      // Create transactions for family 1
      var tx1Family1 = TestHelpers.CreateTestTransaction(id: 600, accountId: 200, vendor: "Vendor 1", familyId: 1);
      tx1Family1.UserId = 100;
      
      // Create transactions for family 2
      var tx1Family2 = TestHelpers.CreateTestTransaction(id: 601, accountId: 201, vendor: "Vendor 2", familyId: 2);
      tx1Family2.UserId = 101;
      
      db.Transactions.AddRange(tx1Family1, tx1Family2);
      await db.SaveChangesAsync();
    }

    // Act
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var transactions = await db.Transactions.Where(t => t.Id >= 600).ToListAsync();
      
      // Assert
      transactions.Should().HaveCount(1, "query filter should only return Family 1 transactions");
      transactions.All(t => t.FamilyId == 1).Should().BeTrue("all transactions should belong to Family 1");
      transactions.First().Id.Should().Be(600, "should only see Family 1 transaction");
    }
  }

  /// <summary>
  /// Test that bank accounts from different families are isolated
  /// </summary>
  [Fact]
  public async Task BankAccounts_Should_Be_Isolated_By_FamilyId()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      
      // Create second family
      var family2 = new Family { Id = 2, Name = "Family 2" };
      db.Families.Add(family2);
      
      // Create accounts
      var account1 = TestHelpers.CreateTestAccount(id: 300, name: "Checking - Family 1", familyId: 1);
      var account2 = TestHelpers.CreateTestAccount(id: 301, name: "Savings - Family 2", familyId: 2);
      db.BankAccounts.AddRange(account1, account2);
      await db.SaveChangesAsync();
    }

    // Act
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var accounts = await db.BankAccounts.Where(a => a.Id >= 300).ToListAsync();
      
      // Assert
      accounts.Should().HaveCount(1, "query filter should only return Family 1 accounts");
      accounts.All(a => a.FamilyId == 1).Should().BeTrue("all accounts should belong to Family 1");
      accounts.First().Id.Should().Be(300, "should only see Family 1 account");
    }
  }

  /// <summary>
  /// Test that categories from different families are isolated
  /// </summary>
  [Fact]
  public async Task Categories_Should_Be_Isolated_By_FamilyId()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      
      // Create second family
      var family2 = new Family { Id = 2, Name = "Family 2" };
      db.Families.Add(family2);
      
      // Create categories
      var cat1 = TestHelpers.CreateTestCategory(id: "400", name: "Category 1 - Family 1", familyId: 1);
      var cat2 = TestHelpers.CreateTestCategory(id: "401", name: "Category 2 - Family 2", familyId: 2);
      db.Categories.AddRange(cat1, cat2);
      await db.SaveChangesAsync();
    }

    // Act
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var categories = await db.Categories.Where(c => int.Parse(c.CategoryId) >= 400).ToListAsync();
      
      // Assert
      categories.Should().HaveCount(1, "query filter should only return Family 1 categories");
      categories.All(c => c.FamilyId == 1).Should().BeTrue("all categories should belong to Family 1");
      categories.First().CategoryId.Should().Be("400", "should only see Family 1 category");
    }
  }
}
