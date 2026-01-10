using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Budget.Api.Features.Transactions;
using Budget.Shared.Models;
using Budget.DB;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Budget.ApiTests;

/// <summary>
/// Tests for Transaction Import API endpoints
/// </summary>
public class TransactionImportEndpointsTests : IntegrationTestBase
{
  /// <summary>
  /// Test ImportTransactions endpoint - should bulk import transactions to staging table
  /// </summary>
  [Fact]
  public async Task ImportTransactions_Should_Bulk_Import_To_Staging_Table()
  {
    // Arrange
    var transactions = new List<TransactionImportDto>
    {
      new()
      {
        Date = DateTime.Today,
        Vendor = "Test Vendor 1",
        Description = "Test Description 1",
        Amount = 100.50m,
        EnvelopeId = 1,
        EnvelopeName = "Groceries",
        UserId = 1
      },
      new()
      {
        Date = DateTime.Today.AddDays(-1),
        Vendor = "Test Vendor 2",
        Description = "Test Description 2",
        Amount = 50.25m,
        EnvelopeId = 2,
        EnvelopeName = "Gas",
        UserId = 1
      }
    };

    var command = new ImportTransactions.Command(transactions);

    // Act
    var response = await Client.PostAsJsonAsync("/Transaction/Import", command);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
    result.Should().NotBeNull();
    result!["count"].Should().Be(2);

    // Verify data in database
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
    var imports = await db.TransactionImports.ToListAsync();
    imports.Should().HaveCount(2);
    imports[0].Vendor.Should().Be("Test Vendor 1");
    imports[1].Vendor.Should().Be("Test Vendor 2");
  }

  /// <summary>
  /// Test GetTransactionImports endpoint - should retrieve staged imports
  /// </summary>
  [Fact]
  public async Task GetTransactionImports_Should_Return_Staged_Imports()
  {
    // Arrange - insert test data directly
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

    var import1 = new TransactionImport
    {
      Date = DateTime.Today,
      Vendor = "Test Vendor A",
      Description = "Test Description A",
      Amount = 75.00m,
      EnvelopeId = 1,
      EnvelopeName = "Dining",
      UserId = 1,
      FamilyId = 1,
      ImportedAt = DateTime.UtcNow
    };

    var import2 = new TransactionImport
    {
      Date = DateTime.Today.AddDays(-2),
      Vendor = "Test Vendor B",
      Description = "Test Description B",
      Amount = 125.50m,
      EnvelopeId = 2,
      EnvelopeName = "Shopping",
      UserId = 1,
      FamilyId = 1,
      ImportedAt = DateTime.UtcNow
    };

    db.TransactionImports.AddRange(import1, import2);
    await db.SaveChangesAsync();

    // Act
    var response = await Client.GetAsync("/Transaction/Import");

    // Assert
    response.EnsureSuccessStatusCode();
    var imports = await response.Content.ReadFromJsonAsync<List<TransactionImportDto>>();
    imports.Should().NotBeNull();
    imports.Should().HaveCount(2);
    imports![0].Vendor.Should().Be("Test Vendor B"); // Ordered by date
    imports[1].Vendor.Should().Be("Test Vendor A");
  }

  /// <summary>
  /// Test ClearTransactionImports endpoint - should clear all staged imports
  /// </summary>
  [Fact]
  public async Task ClearTransactionImports_Should_Clear_All_Staged_Imports()
  {
    // Arrange - insert test data
    using var arrangeScope = _factory.Services.CreateScope();
    var arrangeDb = arrangeScope.ServiceProvider.GetRequiredService<BudgetContext>();

    var import1 = new TransactionImport
    {
      Date = DateTime.Today,
      Vendor = "Test Vendor X",
      Description = "Test Description X",
      Amount = 50.00m,
      EnvelopeId = 1,
      EnvelopeName = "Test",
      UserId = 1,
      FamilyId = 1,
      ImportedAt = DateTime.UtcNow
    };

    arrangeDb.TransactionImports.Add(import1);
    await arrangeDb.SaveChangesAsync();

    // Act
    var response = await Client.DeleteAsync("/Transaction/Import");

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
    result.Should().NotBeNull();
    result!["count"].Should().Be(1);

    // Verify data is cleared
    using var assertScope = _factory.Services.CreateScope();
    var assertDb = assertScope.ServiceProvider.GetRequiredService<BudgetContext>();
    var imports = await assertDb.TransactionImports.ToListAsync();
    imports.Should().BeEmpty();
  }

  /// <summary>
  /// Test duplicate detection - should mark duplicates after import
  /// </summary>
  [Fact]
  public async Task ImportTransactions_Should_Detect_Duplicates()
  {
    // Arrange - Create an existing transaction
    using var arrangeScope = _factory.Services.CreateScope();
    var arrangeDb = arrangeScope.ServiceProvider.GetRequiredService<BudgetContext>();

    var account = TestHelpers.CreateTestAccount(id: 300, balance: 1000m);
    arrangeDb.BankAccounts.Add(account);

    var envelope = TestHelpers.CreateTestEnvelope(id: 300, categoryId: "1", balance: 500m);
    arrangeDb.Envelopes.Add(envelope);

    var existingTransaction = new Transaction
    {
      Date = DateTime.Today,
      Vendor = "Duplicate Vendor",
      TotalAmount = 75.00m,
      AccountId = account.Id,
      UserId = 1,
      FamilyId = 1
    };
    arrangeDb.Transactions.Add(existingTransaction);
    await arrangeDb.SaveChangesAsync();

    // Import new transactions with one duplicate
    var transactions = new List<TransactionImportDto>
    {
      new()
      {
        Date = DateTime.Today,
        Vendor = "Duplicate Vendor",
        Description = "This should be marked as duplicate",
        Amount = 75.00m,
        EnvelopeId = 1,
        EnvelopeName = "Test",
        UserId = 1
      },
      new()
      {
        Date = DateTime.Today,
        Vendor = "New Vendor",
        Description = "This should NOT be marked as duplicate",
        Amount = 50.00m,
        EnvelopeId = 1,
        EnvelopeName = "Test",
        UserId = 1
      }
    };

    var command = new ImportTransactions.Command(transactions);

    // Act
    var response = await Client.PostAsJsonAsync("/Transaction/Import", command);

    // Assert
    response.EnsureSuccessStatusCode();

    using var assertScope = _factory.Services.CreateScope();
    var assertDb = assertScope.ServiceProvider.GetRequiredService<BudgetContext>();
    var imports = await assertDb.TransactionImports.ToListAsync();
    
    imports.Should().HaveCount(2);
    imports.First(i => i.Vendor == "Duplicate Vendor").Duplicate.Should().BeTrue();
    imports.First(i => i.Vendor == "New Vendor").Duplicate.Should().BeFalse();
  }

  /// <summary>
  /// Test UpdateTransactionImport endpoint - should update duplicate flag
  /// </summary>
  [Fact]
  public async Task UpdateTransactionImport_Should_Update_Duplicate_Flag()
  {
    // Arrange
    using var arrangeScope = _factory.Services.CreateScope();
    var arrangeDb = arrangeScope.ServiceProvider.GetRequiredService<BudgetContext>();

    var import = new TransactionImport
    {
      Date = DateTime.Today,
      Vendor = "Test Vendor",
      Description = "Test",
      Amount = 100m,
      EnvelopeId = 1,
      EnvelopeName = "Test",
      UserId = 1,
      FamilyId = 1,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };

    arrangeDb.TransactionImports.Add(import);
    await arrangeDb.SaveChangesAsync();
    var importId = import.Id;

    // Act - Update to mark as duplicate
    var payload = new { Duplicate = true };
    var response = await Client.PutAsJsonAsync($"/Transaction/Import/{importId}", payload);

    // Assert
    response.EnsureSuccessStatusCode();

    using var assertScope = _factory.Services.CreateScope();
    var assertDb = assertScope.ServiceProvider.GetRequiredService<BudgetContext>();
    var updatedImport = await assertDb.TransactionImports.FindAsync(importId);
    
    updatedImport.Should().NotBeNull();
    updatedImport!.Duplicate.Should().BeTrue();
  }
}
