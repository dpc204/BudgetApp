using Budget.Api.Features.Envelopes.EnvelopeMaint;
using Budget.DB;
using FastEndpoints;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Budget.Shared.Models;
using Xunit;
using EnvelopeGetAll = Budget.Api.Features.Envelopes.GetAll;

namespace Budget.ApiTests;

public class EnvelopeEndpointTests2 : IntegrationTestBase
{
  /// <summary>
  /// Test GetAll (envelopes/getall) endpoint - should return all envelopes with categories
  /// </summary>
  [Fact]
  public async Task GetAllEnvelopes_Should_Return_All_Envelopes()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var envelope1 = TestHelpers.CreateTestEnvelope(id: 400, name: "Groceries", categoryId: "1", balance: 100m);
      var envelope2 = TestHelpers.CreateTestEnvelope(id: 401, name: "Gas", categoryId: "1", balance: 50m);

      db.Envelopes.Add(envelope1);
      db.Envelopes.Add(envelope2);
      await db.SaveChangesAsync();

      // Act
      var response = await Client.GetAsync("/envelopes/getall");

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<List<EnvelopeGetAll.Response>>();

      result.Should().NotBeNull();
      result.Should().HaveCount(c => c >= 2);

      var env1 = result!.FirstOrDefault(e => e.Id == 400);
      env1.Should().NotBeNull();
      env1!.Name.Should().Be("Groceries");
      env1.Balance.Should().Be(100m);
    }
  }



  /// <summary>
  /// Test GetEnvelope (envelopes/maint/getall) endpoint - should return all envelopes
  /// </summary>
  [Fact]
  public async Task GetEnvelope_Should_Return_All_Envelopes_With_Details()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      var envelope = TestHelpers.CreateTestEnvelope(id: 402, name: "Test Envelope", categoryId: "1", balance: 200m);
      db.Envelopes.Add(envelope);
      await db.SaveChangesAsync();

      // Act
      var response = await Client.GetAsync("/envelopes/maint/getall");

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<List<GetAll.Response>>();

      result.Should().NotBeNull();
      result.Should().HaveCount(c => c >= 1);

      var env = result!.FirstOrDefault(e => e.Id == 402);
      env.Should().NotBeNull();
      env!.Name.Should().Be("Test Envelope");
      env.Balance.Should().Be(200m);
    }
  }

  /// <summary>
  /// Test InsertEnvelope endpoint - should create a new envelope
  /// </summary>
  [Fact]
  public async Task InsertEnvelope_Should_Create_New_Envelope()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var command = new InsertEnvelope.Command(
          Name: "New Envelope",
          Description: "Test description",
          Balance: 150m,
          Budget: 200m,
          CategoryId: "1",
          SortOrder: 10);

      // Act
      var response = await Client.PostAsJsonAsync("/envelopes/maint/Insert", command);

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<InsertEnvelope.Response>();

      result.Should().NotBeNull();
      result!.Name.Should().Be("New Envelope");
      result.Balance.Should().Be(150m);
      result.Budget.Should().Be(200m);
      result.Id.Should().BeGreaterThan(0);

      // Verify in database
      db.ChangeTracker.Clear();
      var savedEnvelope = await db.Envelopes.FindAsync(result.Id);

      savedEnvelope.Should().NotBeNull();
      savedEnvelope!.Name.Should().Be("New Envelope");
    }
  }

  /// <summary>
  /// Test UpdateEnvelope endpoint - should update an existing envelope
  /// </summary>
  [Fact]
  public async Task UpdateEnvelope_Should_Update_Existing_Envelope()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      var envelope = TestHelpers.CreateTestEnvelope(id: 403, name: "Original Name", categoryId: "1", balance: 100m);
      db.Envelopes.Add(envelope);
      await db.SaveChangesAsync();

      var commandBody = new EnvelopeDto()
      {
        Id = 403,
        Name = "Updated Name",
        Description = "Updated description",
        Balance = 250m,
        Budget = 300m,
        CategoryId = "1",
        SortOrder = 5
      };

      // Act
      var response = await Client.PutAsJsonAsync("/envelopes/maint/403", commandBody);

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<UpdateEnvelope.Response>();

      result.Should().NotBeNull();
      result!.envelope.Id.Should().Be(403);
      result.envelope.Name.Should().Be("Updated Name");
      result.envelope.Balance.Should().Be(250m);
      result.envelope.Budget.Should().Be(300m);

      // Verify in database
      db.ChangeTracker.Clear();
      var updatedEnvelope = await db.Envelopes.FindAsync(403);

      updatedEnvelope.Should().NotBeNull();
      updatedEnvelope!.Name.Should().Be("Updated Name");
      updatedEnvelope.Balance.Should().Be(250m);
    }
  }

  /// <summary>
  /// Test UpdateEnvelope endpoint with mismatched IDs - should return BadRequest
  /// </summary>
  [Fact]
  public async Task UpdateEnvelope_With_Mismatched_Ids_Should_Return_BadRequest()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var commandBody = new UpdateEnvelope.CommandBody
      {
        Id = 999,
        Name = "Test",
        Description = "Test",
        Balance = 100m,
        Budget = null,
        CategoryId = "1",
        SortOrder = 1
      };

      // Act
      var response = await Client.PutAsJsonAsync("/envelopes/maint/404", commandBody);

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
  }

  /// <summary>
  /// Test RemoveEnvelope endpoint - should delete an envelope
  /// </summary>
  [Fact]
  public async Task RemoveEnvelope_Should_Delete_Envelope()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      var envelope = TestHelpers.CreateTestEnvelope(id: 405, name: "To Delete", categoryId: "1", balance: 50m);
      db.Envelopes.Add(envelope);
      await db.SaveChangesAsync();

      // Act
      var response = await Client.DeleteAsync("/envelopes/maint/405");

      // Assert
      response.EnsureSuccessStatusCode();
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

      // Verify deletion in database
      db.ChangeTracker.Clear();
      var deletedEnvelope = await db.Envelopes.FindAsync(405);
      deletedEnvelope.Should().BeNull();
    }
  }

  /// <summary>
  /// Test RemoveEnvelope endpoint with non-existent envelope - should return NotFound
  /// </summary>
  [Fact]
  public async Task RemoveEnvelope_With_NonExistent_Envelope_Should_Return_NotFound()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      // Act
      var response = await Client.DeleteAsync("/envelopes/maint/99999");

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
  }

  /// <summary>
  /// Test GetEnvelopeTransactionCount endpoint - should return transaction count for envelope
  /// </summary>
  [Fact]
  public async Task GetEnvelopeTransactionCount_Should_Return_Transaction_Count()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      var account = TestHelpers.CreateTestAccount(id: 406, balance: 1000m);
      db.BankAccounts.Add(account);

      var envelope = TestHelpers.CreateTestEnvelope(id: 406, name: "Test Envelope", categoryId: "1", balance: 500m);
      db.Envelopes.Add(envelope);

      var details = new List<TransactionDetail>
      {
        TestHelpers.CreateTestTransactionDetail(transactionId: 406, lineId: 1, envelopeId: envelope.Id, amount: 50m),
        TestHelpers.CreateTestTransactionDetail(transactionId: 407, lineId: 1, envelopeId: envelope.Id, amount: 75m)
      };

      var transaction1 = TestHelpers.CreateTestTransaction(id: 406, accountId: account.Id, totalAmount: 50m, details: [details[0]]);
      var transaction2 = TestHelpers.CreateTestTransaction(id: 407, accountId: account.Id, totalAmount: 75m, details: [details[1]]);

      db.Transactions.Add(transaction1);
      db.Transactions.Add(transaction2);
      await db.SaveChangesAsync();

      // Act
      var response = await Client.GetAsync($"/envelopes/maint/{envelope.Id}/transaction-count");

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<GetEnvelopeTransactionCount.Response>();

      result.Should().NotBeNull();
      result!.EnvelopeId.Should().Be(envelope.Id);
      result.TransactionCount.Should().Be(2);
    }
  }

  /// <summary>
  /// Test ImportEnvelopes endpoint - should import envelopes from CSV
  /// </summary>
  [Fact]
  public async Task ImportEnvelopes_Should_Import_From_CSV()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var csvContent = "Name,Description,Balance,Budget,CategoryId,SortOrder\nImported Env 1,Desc 1,100,200,1,1\nImported Env 2,Desc 2,150,250,1,2";

      var request = new ImportEnvelopes.ImportRequest
      {
        CsvContent = csvContent
      };

      // Act
      var response = await Client.PostAsJsonAsync("/envelopes/maint/import", request);

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<ImportEnvelopes.Response>();

      result.Should().NotBeNull();
      result!.ImportedCount.Should().BeGreaterThan(0);
      result.Errors.Should().BeEmpty();
    }
  }
}
