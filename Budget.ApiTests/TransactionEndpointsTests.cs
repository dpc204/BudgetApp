using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Budget.Api.Features.Transactions;
using Budget.DB;
using Budget.Shared.Enums;
using Budget.Shared.Models;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace Budget.ApiTests;

/// <summary>
/// Tests for Transaction API endpoints
/// </summary>
public class TransactionEndpointsTests : IntegrationTestBase
{
  //private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  //  => new DbContextOptionsBuilder<BudgetContext>()
  //    .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
  //    .Options;

  [Fact]
  public async Task AddNewTransaction_Should_Create_Transaction_And_Update_Balances()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var account = new BankAccount { Id = 200, Name = "Test Account", Balance = 1000m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope 
    { 
      Id = 200, 
      Name = "Test Envelope", 
      CategoryId = "1", 
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Users.Add(user);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var transactionDetail = new OneTransactionDetail
    {
      AccountId = account.Id,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      UserId = 1,
      Details =
      [
        new TransactionDetailDto
        {
          TransactionId = 0,
          EnvelopeId = envelope.Id,
          Amount = -100m,
          Notes = "Test purchase"
          // LineId will be assigned by backend
        }
      ]
    };
     
    var mockFamilyService = new TestCurrentFamilyService(1);
    var inserter = new InsertTransactions(context, mockFamilyService);
    var handler = new AddNewTransaction.Handler( inserter);
    var command = new AddNewTransaction.Command(transactionDetail);

    // Act
    TransactionAddResult result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    
    context.ChangeTracker.Clear();
    BankAccount? updatedAccount = await context.BankAccounts.FindAsync([account.Id], TestContext.Current.CancellationToken);
    Envelope? updatedEnvelope = await context.Envelopes.FindAsync([envelope.Id], TestContext.Current.CancellationToken);

    updatedAccount.Should().NotBeNull();
    updatedAccount!.Balance.Should().Be(900m); // 1000 - 100

    updatedEnvelope.Should().NotBeNull();
    updatedEnvelope!.Balance.Should().Be(400m); // 500 - 100
  }

  [Fact]
  public async Task GetUnassigned_Should_Return_Unallocated_Transactions()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "-2", Name = "Unassigned", Description = "Unassigned", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.System };
    var account = new BankAccount { Id = 201, Name = "Test Account", Balance = 1000m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var unassignedEnvelope = new Envelope 
    { 
      Id = -2, 
      Name = "Unassigned", 
      CategoryId = "-2", 
      Balance = 0m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Unassigned,
      SortOrder = 999
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(unassignedEnvelope);
    context.Users.Add(user);
    
    var transaction = new Transaction 
    { 
      Id = 300, 
      AccountId = 201, 
      Vendor = "Unassigned Vendor", 
      Date = DateTime.UtcNow,
      TotalAmount = 50m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail 
    { 
      TransactionId = 300, 
      LineId = 1, 
      EnvelopeId = -2, 
      Amount = 50m,
      Notes = "Unassigned"
    };
    
    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetUnassigned.Handler(context);

    // Act
    Result<IEnumerable<GetUnassigned.Response>> result = await handler.Handle(new GetUnassigned.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    var resultList = result.Value.ToList();
    resultList.Should().HaveCountGreaterThanOrEqualTo(1);
    resultList.Should().Contain(t => t.TransactionId == 300);
  }

  [Fact]
  public async Task GetByEnvelopeId_Should_Return_Transactions_For_Envelope()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var account = new BankAccount { Id = 202, Name = "Test Account", Balance = 1000m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope 
    { 
      Id = 202, 
      Name = "Groceries", 
      CategoryId = "1", 
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Users.Add(user);
    
    var transaction = new Transaction 
    { 
      Id = 400, 
      AccountId = 202, 
      Vendor = "Store", 
      Date = DateTime.UtcNow,
      TotalAmount = 75m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail 
    { 
      TransactionId = 400, 
      LineId = 1, 
      EnvelopeId = 202, 
      Amount = 75m,
      Notes = "Groceries"
    };
    
    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetByEnvelopeId.Handler(context);

    // Act
    IEnumerable<GetByEnvelopeId.Response> result = await handler.Handle(new GetByEnvelopeId.Query(202), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var resultList = result.ToList();
    resultList.Should().HaveCount(1);
    resultList[0].TransactionId.Should().Be(400);
    resultList[0].Amount.Should().Be(75m);
  }

  [Fact]
  public async Task GetOneTransactionDetail_Should_Return_Transaction_Details()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var account = new BankAccount { Id = 203, Name = "Test Account", Balance = 1000m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope 
    { 
      Id = 203, 
      Name = "Test Envelope", 
      CategoryId = "1", 
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Users.Add(user);
    
    var transaction = new Transaction 
    { 
      Id = 500, 
      AccountId = 203, 
      Vendor = "Test Vendor", 
      Date = DateTime.UtcNow,
      TotalAmount = 100m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail 
    { 
      TransactionId = 500, 
      LineId = 1, 
      EnvelopeId = 203, 
      Amount = 100m,
      Notes = "Test"
    };
    
    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetOneTransactionDetail.Handler(context);

    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(new GetOneTransactionDetail.Query(500), CancellationToken.None);

    // Assert

      result.Should().NotBeNull();
    if(result != null)
    {
      result?.Id.Should().Be(500);
      result?.AccountId.Should().Be(203);
      result?.Vendor.Should().Be("Test Vendor");
      result?.Details.Should().HaveCount(1);
      result?.Details[0].Amount.Should().Be(100m);
    }
  }

  [Fact]
  public async Task AssignTransaction_Should_Reassign_Transaction_Detail()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var unallocatedCategory = new Category { CategoryId = "-1", Name = "UnAllocated", Description = "UnAllocated", SortOrder = 999, FamilyId = 1, CategoryType = CatTypes.System };
    var account = new BankAccount { Id = 204, Name = "Test Account", Balance = 1000m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope 
    { 
      Id = 204, 
      Name = "Groceries", 
      CategoryId = "1", 
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var unallocatedEnvelope = new Envelope 
    { 
      Id = -1, 
      Name = "UnAllocated", 
      CategoryId = "-1", 
      Balance = 50m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Income,
      SortOrder = 999
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Categories.AddRange(category, unallocatedCategory);
    context.BankAccounts.Add(account);
    context.Envelopes.AddRange(envelope, unallocatedEnvelope);
    context.Users.Add(user);
    
    var transaction = new Transaction 
    { 
      Id = 600, 
      AccountId = 204, 
      Vendor = "Store", 
      Date = DateTime.UtcNow,
      TotalAmount = 50m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail 
    { 
      TransactionId = 600, 
      LineId = 1, 
      EnvelopeId = -1, 
      Amount = 50m,
      Notes = "Unassigned"
    };
    
    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new AssignTransaction.Handler(context, new MoveEnvelopeBalance());
    var command = new AssignTransaction.Command(600, 1, 204, "Updated Vendor", "Reassigned", "notes");

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeTrue();

    context.ChangeTracker.Clear();
    TransactionDetail? updatedDetail = await context.TransactionDetails.FindAsync([600, 1], TestContext.Current.CancellationToken);
    updatedDetail.Should().NotBeNull();
    updatedDetail!.EnvelopeId.Should().Be(204);
  }

  [Fact]
  public async Task UpdateTransaction_Should_Update_Transaction_And_Recalculate_Balances()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var account = new BankAccount { Id = 205, Name = "Test Account", Balance = 900m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope 
    { 
      Id = 205, 
      Name = "Test Envelope", 
      CategoryId = "1", 
      Balance = 400m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Users.Add(user);
    
    var transaction = new Transaction 
    { 
      Id = 700, 
      AccountId = 205, 
      Vendor = "Original Vendor", 
      Date = DateTime.UtcNow,
      TotalAmount = 100m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail 
    { 
      TransactionId = 700, 
      LineId = 1, 
      EnvelopeId = 205, 
      Amount = 100m,
      Notes = "Original"
    };
    
    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateTransaction.Handler(context);
    var updatedTransaction = new OneTransactionDetail
    {
      Id = 700,
      AccountId = 205,
      Date = DateTime.UtcNow,
      Vendor = "Updated Vendor",
      UserId = 1,
      Details =
      [
        new TransactionDetailDto
        {
          TransactionId = 700,
          LineId = 1,
          EnvelopeId = 205,
          Amount = 150m,
          Notes = "Updated"
        }
      ]
    };
    var command = new UpdateTransaction.Command( updatedTransaction);

    // Act
    Result<List<EnvelopeDto>> result = await handler.Handle(command, CancellationToken.None);

    // Assert
    EnvelopeDto? testResult = result.Value.FirstOrDefault();
    testResult?.Should().NotBeNull();
    testResult?.Balance.Should().Be(350);
    testResult?.Id.Should().Be(205);
    
    context.ChangeTracker.Clear();
    Transaction? updatedTx = await context.Transactions.FindAsync([700], TestContext.Current.CancellationToken);
    updatedTx.Should().NotBeNull();
    updatedTx!.Vendor.Should().Be("Updated Vendor");
    updatedTx.TotalAmount.Should().Be(150m);
  }

  [Fact]
  public async Task UpdateTransaction_Should_Persist_Description()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var account = new BankAccount { Id = 206, Name = "Test Account", Balance = 900m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope
    {
      Id = 206,
      Name = "Test Envelope",
      CategoryId = "1",
      Balance = 400m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Users.Add(user);

    var transaction = new Transaction
    {
      Id = 800,
      AccountId = 206,
      Vendor = "Original Vendor",
      Description = "Original Description",
      Date = DateTime.UtcNow,
      TotalAmount = 100m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail
    {
      TransactionId = 800,
      LineId = 1,
      EnvelopeId = 206,
      Amount = 100m,
      Notes = "Original Note"
    };

    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateTransaction.Handler(context);
    var updatedTransaction = new OneTransactionDetail
    {
      Id = 800,
      AccountId = 206,
      Date = DateTime.UtcNow,
      Vendor = "Updated Vendor",
      Description = "Updated Description",
      UserId = 1,
      Details =
      [
        new TransactionDetailDto
        {
          TransactionId = 800,
          LineId = 1,
          EnvelopeId = 206,
          Amount = 100m,
          Notes = "Updated Note"
        }
      ]
    };
    var command = new UpdateTransaction.Command(updatedTransaction);

    // Act
    var result = await handler.Handle(command, TestContext.Current.CancellationToken);

    // Assert
    result.IsSuccess.Should().BeTrue();

    context.ChangeTracker.Clear();
    var updatedTx = await context.Transactions.FindAsync([800], TestContext.Current.CancellationToken);
    updatedTx.Should().NotBeNull();
    updatedTx!.Vendor.Should().Be("Updated Vendor");
    updatedTx.Description.Should().Be("Updated Description");
  }

  [Fact]
  public async Task UpdateTransaction_Should_Return_NotFound_For_NonExistent_Transaction()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var handler = new UpdateTransaction.Handler(context);
    var transaction = new OneTransactionDetail
    {
      Id = 99999,
      AccountId = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test",
      UserId = 1,
      Details = []
    };
    var command = new UpdateTransaction.Command(transaction);

    // Act
    Result<List<EnvelopeDto>> result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailed.Should().Be(true);
  }
}

