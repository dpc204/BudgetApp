using System;
using System.Linq;
using System.Threading.Tasks;
using GetAllEnvelopes = Budget.Api.Features.Envelopes.EnvelopeMaint.GetAll;
using Budget.Api.Features.Envelopes;
using Budget.Api.Features.Envelopes.EnvelopeMaint;
using Budget.DB;
using Budget.Shared.Enums;
using Budget.Shared.Models;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace Budget.ApiTests;

public class EnvelopeEndpointTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    => new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
      .Options;

  [Fact]
  public async Task GetAllEnvelopes_Should_Return_All_Envelopes()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test Category", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var envelope1 = new Envelope 
    { 
      Id = 400, 
      Name = "Groceries", 
      CategoryId = "1", 
      Balance = 100m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var envelope2 = new Envelope 
    { 
      Id = 401, 
      Name = "Gas", 
      CategoryId = "1", 
      Balance = 50m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 2
    };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.Envelopes.AddRange(envelope1, envelope2);
    await context.SaveChangesAsync();

    var handler = new GetAllCategories.Handler(context);

    // Act
    var result = await handler.Handle(new GetAllCategories.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var resultList = result.ToList();
    resultList.Should().HaveCount(2);

    var env1 = resultList.Should().ContainSingle(e => e.Id == 400).Subject;
    env1.Name.Should().Be("Groceries");
    env1.Balance.Should().Be(100m);
  }

  [Fact]
  public async Task GetEnvelope_Should_Return_All_Envelopes_With_Details()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test Category", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var envelope = new Envelope 
    { 
      Id = 402, 
      Name = "Test Envelope", 
      CategoryId = "1", 
      Balance = 200m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync();

    var handler = new GetAllEnvelopes.Handler(context);

    // Act
    var result = await handler.Handle(new GetAllEnvelopes.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var resultList = result.ToList();
    resultList.Should().HaveCount(1);

    var env = resultList.Should().ContainSingle(e => e.Id == 402).Subject;
    env.Name.Should().Be("Test Envelope");
    env.Balance.Should().Be(200m);
  }

  [Fact]
  public async Task InsertEnvelope_Should_Create_New_Envelope()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test Category", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    await context.SaveChangesAsync();

    var handler = new InsertEnvelope.Handler(context);
    var command = new InsertEnvelope.Command(
      Name: "New Envelope",
      Description: "Test description",
      Balance: 150m,
      Budget: 200m,
      CategoryId: "1",
      SortOrder: 10);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be("New Envelope");
    result.Balance.Should().Be(150m);
    result.Budget.Should().Be(200m);
    result.Id.Should().BeGreaterThan(0);

    // Verify in database
    var savedEnvelope = await context.Envelopes.FindAsync(result.Id);
    savedEnvelope.Should().NotBeNull();
    savedEnvelope!.Name.Should().Be("New Envelope");
    savedEnvelope.Description.Should().Be("Test description");
  }

  [Fact]
  public async Task UpdateEnvelope_Should_Update_Existing_Envelope()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test Category", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var envelope = new Envelope 
    { 
      Id = 403, 
      Name = "Original Name", 
      CategoryId = "1", 
      Balance = 100m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync();

    var handler = new UpdateEnvelope.Handler(context);
    var updateDto = new EnvelopeUpdateDto
    {
      Id = 403,
      Name = "Updated Name",
      Description = "Updated description",
      Budget = 300m,
      CategoryId = "1",
      SortOrder = 5
    };
    var command = new UpdateEnvelope.Command(updateDto);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.envelope.Id.Should().Be(403);
    result.envelope.Name.Should().Be("Updated Name");
    result.envelope.Budget.Should().Be(300m);

    // Verify in database
    context.ChangeTracker.Clear();
    var updatedEnvelope = await context.Envelopes.FindAsync(403);
    updatedEnvelope.Should().NotBeNull();
    updatedEnvelope!.Name.Should().Be("Updated Name");
    updatedEnvelope.Budget.Should().Be(300m);
    updatedEnvelope.Balance.Should().Be(100m);  // Balance should not be changed
  }

  [Fact]
  public async Task UpdateEnvelope_With_Mismatched_Ids_Should_Return_Null()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var handler = new UpdateEnvelope.Handler(context);
    var updateDto = new EnvelopeUpdateDto
    {
      Id = 999,
      Name = "Test",
      Description = "Test",
      Budget = null,
      CategoryId = "1",
      SortOrder = 1
    };
    var command = new UpdateEnvelope.Command(updateDto);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task RemoveEnvelope_Should_Delete_Envelope()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test Category", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var envelope = new Envelope 
    { 
      Id = 405, 
      Name = "To Delete", 
      CategoryId = "1", 
      Balance = 50m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync();

    var handler = new RemoveEnvelope.Handler(context);

    // Act
    var result = await handler.Handle(new RemoveEnvelope.Command(405), CancellationToken.None);

    // Assert
    result.Should().BeTrue();

    // Verify deletion in database
    context.ChangeTracker.Clear();
    var deletedEnvelope = await context.Envelopes.FindAsync(405);
    deletedEnvelope.Should().BeNull();
  }

  [Fact]
  public async Task RemoveEnvelope_With_NonExistent_Envelope_Should_Return_False()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var handler = new RemoveEnvelope.Handler(context);

    // Act
    var result = await handler.Handle(new RemoveEnvelope.Command(99999), CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public async Task GetEnvelopeTransactionCount_Should_Return_Transaction_Count()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test Category", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var account = new BankAccount { Id = 406, Name = "Test Account", Balance = 1000m, AccountType = BankAccount.AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope 
    { 
      Id = 406, 
      Name = "Test Envelope", 
      CategoryId = "1", 
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync();

    var transaction1 = new Transaction 
    { 
      Id = 406, 
      AccountId = account.Id, 
      TotalAmount = 50m, 
      Date = DateTime.Now,
      FamilyId = 1
    };
    var transaction2 = new Transaction 
    { 
      Id = 407, 
      AccountId = account.Id, 
      TotalAmount = 75m, 
      Date = DateTime.Now,
      FamilyId = 1
    };

    var detail1 = new TransactionDetail 
    { 
      TransactionId = 406, 
      LineId = 1, 
      EnvelopeId = envelope.Id, 
      Amount = 50m
    };
    var detail2 = new TransactionDetail 
    { 
      TransactionId = 407, 
      LineId = 1, 
      EnvelopeId = envelope.Id, 
      Amount = 75m
    };

    context.Transactions.AddRange(transaction1, transaction2);
    context.TransactionDetails.AddRange(detail1, detail2);
    await context.SaveChangesAsync();

    var handler = new GetEnvelopeTransactionCount.Handler(context);

    // Act
    var result = await handler.Handle(new GetEnvelopeTransactionCount.Query(envelope.Id), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.EnvelopeId.Should().Be(envelope.Id);
    result.TransactionCount.Should().Be(2);
  }

  [Fact]
  public async Task ImportEnvelopes_Should_Import_From_CSV()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test Category", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    
    context.Families.Add(family);
    context.Categories.Add(category);
    await context.SaveChangesAsync();

    var csvContent = "Name,Description,Balance,Budget,CategoryId,SortOrder\nImported Env 1,Desc 1,100,200,1,1\nImported Env 2,Desc 2,150,250,1,2";

    var handler = new ImportEnvelopes.Handler(context, NullLogger<ImportEnvelopes.Handler>.Instance);
    var command = new ImportEnvelopes.Command(csvContent);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.ImportedCount.Should().BeGreaterThan(0);
    result.Errors.Should().BeEmpty();
  }
}
