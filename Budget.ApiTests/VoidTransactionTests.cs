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
using Xunit;

namespace Budget.ApiTests;

/// <summary>
/// Tests for the VoidTransaction API endpoint
/// </summary>
public class VoidTransactionTests : IntegrationTestBase
{

  /// <summary>
  /// Test that voiding a transaction adds the amount back to the BankAccount balance
  /// </summary>
  [Fact]
  public async Task VoidTransaction_Should_Reverse_BankAccount_Balance()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      // Create test data
      var account = TestHelpers.CreateTestAccount(id: 100, balance: 1000m);
      db.BankAccounts.Add(account);

      var envelope = TestHelpers.CreateTestEnvelope(id: 100, categoryId: "1", balance: 500m);
      db.Envelopes.Add(envelope);

      var details = new List<TransactionDetail>
      {
        TestHelpers.CreateTestTransactionDetail(
          transactionId: 100,
          lineId: 1,
          envelopeId: envelope.Id,
          amount: 100m,
          notes: "Test transaction")
      };

      var transaction = TestHelpers.CreateTestTransaction(
          id: 100,
          accountId: account.Id,
          vendor: "Test Vendor",
          totalAmount: 100m,
          isVoided: false,
          details: details);

      db.Transactions.Add(transaction);

      // Simulate the balance reduction that would have happened when the transaction was created
      account.Balance -= transaction.TotalAmount; // Balance should be 900
      envelope.Balance -= 100m; // Balance should be 400

      await db.SaveChangesAsync();

      var initialAccountBalance = account.Balance;
      var initialEnvelopeBalance = envelope.Balance;

      // Act
      var command = new VoidTransaction.Command(transaction.Id);
      var response = await Client.PostAsJsonAsync("/Transaction/Void", command);

      // Assert
      response.EnsureSuccessStatusCode();

      // Verify the response contains the updated envelope data
      var result = await response.Content.ReadFromJsonAsync<List<EnvelopeDto>>();
      result.Should().NotBeNull();
      result.Should().HaveCount(1);
      result![0].Id.Should().Be(envelope.Id);
      result[0].Balance.Should().Be(500m); // Restored to original

      // Clear change tracker to force reload from database
      db.ChangeTracker.Clear();

      // Reload entities from database
      var updatedAccount = await db.BankAccounts.FindAsync(account.Id);
      var updatedTransaction = await db.Transactions.FindAsync(transaction.Id);

      updatedAccount.Should().NotBeNull();
      updatedAccount!.Balance.Should().Be(initialAccountBalance + transaction.TotalAmount);
      updatedAccount.Balance.Should().Be(1000m); // Back to original 1000

      updatedTransaction.Should().NotBeNull();
      updatedTransaction!.IsVoided.Should().BeTrue();
    }
  }

  /// <summary>
  /// Test that voiding a transaction adds the amount back to the Envelope balance
  /// </summary>
  [Fact]
  public async Task VoidTransaction_Should_Reverse_Envelope_Balance()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      // Create test data
      var account = TestHelpers.CreateTestAccount(id: 101, balance: 2000m);
      db.BankAccounts.Add(account);

      var envelope = TestHelpers.CreateTestEnvelope(id: 101, categoryId: "1", balance: 800m);
      db.Envelopes.Add(envelope);

      var details = new List<TransactionDetail>
      {
        TestHelpers.CreateTestTransactionDetail(
          transactionId: 101,
          lineId: 1,
          envelopeId: envelope.Id,
          amount: 75m,
          notes: "Test transaction detail")
      };

      var transaction = TestHelpers.CreateTestTransaction(
          id: 101,
          accountId: account.Id,
          vendor: "Test Vendor 2",
          totalAmount: 75m,
          isVoided: false,
          details: details);

      db.Transactions.Add(transaction);

      // Simulate the balance reduction that would have happened when the transaction was created
      account.Balance -= transaction.TotalAmount;
      envelope.Balance -= 75m; // Balance should be 725

      await db.SaveChangesAsync();

      var initialEnvelopeBalance = envelope.Balance;

      // Act
      var command = new VoidTransaction.Command(transaction.Id);
      var response = await Client.PostAsJsonAsync("/Transaction/Void", command);

      // Assert
      response.EnsureSuccessStatusCode();

      // Clear change tracker to force reload from database
      db.ChangeTracker.Clear();

      var result = await response.Content.ReadFromJsonAsync<List<EnvelopeDto>>();
      result.Should().NotBeNull();
      result.Should().HaveCount(1);
      result![0].Balance.Should().Be(initialEnvelopeBalance + 75m);
      result[0].Balance.Should().Be(800m); // Back to original 800
    }
  }

  /// <summary>
  /// Test that voiding a transaction with multiple envelope details correctly reverses all balances
  /// </summary>
  [Fact]
  public async Task VoidTransaction_Should_Reverse_Multiple_Envelope_Balances()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      // Create test data
      var account = TestHelpers.CreateTestAccount(id: 102, balance: 3000m);
      db.BankAccounts.Add(account);

      var envelope1 = TestHelpers.CreateTestEnvelope(id: 102, categoryId: "1", balance: 1000m);
      db.Envelopes.Add(envelope1);

      var envelope2 = TestHelpers.CreateTestEnvelope(id: 103, categoryId: "1", balance: 500m);
      db.Envelopes.Add(envelope2);

      var details = new List<TransactionDetail>
      {
        TestHelpers.CreateTestTransactionDetail(
          transactionId: 102,
          lineId: 1,
          envelopeId: envelope1.Id,
          amount: 100m,
          notes: "First detail"),
        TestHelpers.CreateTestTransactionDetail(
          transactionId: 102,
          lineId: 2,
          envelopeId: envelope2.Id,
          amount: 50m,
          notes: "Second detail")
      };

      var transaction = TestHelpers.CreateTestTransaction(
          id: 102,
          accountId: account.Id,
          vendor: "Test Vendor 3",
          totalAmount: 150m,
          isVoided: false,
          details: details);

      db.Transactions.Add(transaction);

      // Simulate the balance reduction that would have happened when the transaction was created
      account.Balance -= transaction.TotalAmount; // 3000 - 150 = 2850
      envelope1.Balance -= 100m; // 1000 - 100 = 900
      envelope2.Balance -= 50m; // 500 - 50 = 450

      await db.SaveChangesAsync();

      // Act
      var command = new VoidTransaction.Command(transaction.Id);
      var response = await Client.PostAsJsonAsync("/Transaction/Void", command);

      // Assert
      response.EnsureSuccessStatusCode();

      // Clear change tracker to force reload from database
      db.ChangeTracker.Clear();

      var result = await response.Content.ReadFromJsonAsync<List<EnvelopeDto>>();
      result.Should().NotBeNull();
      result.Should().HaveCount(2);

      var env1Result = result!.FirstOrDefault(e => e.Id == envelope1.Id);
      var env2Result = result!.FirstOrDefault(e => e.Id == envelope2.Id);

      env1Result.Should().NotBeNull();
      env1Result!.Balance.Should().Be(1000m); // Back to original

      env2Result.Should().NotBeNull();
      env2Result!.Balance.Should().Be(500m); // Back to original

      // Verify account balance
      var updatedAccount = await db.BankAccounts.FindAsync(account.Id);
      updatedAccount.Should().NotBeNull();
      updatedAccount!.Balance.Should().Be(3000m); // Back to original
    }
  }

  /// <summary>
  /// Test that attempting to void an already voided transaction returns 409 Conflict
  /// </summary>
  [Fact]
  public async Task VoidTransaction_Should_Return_Conflict_When_Already_Voided()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      // Create test data with an already voided transaction
      var account = TestHelpers.CreateTestAccount(id: 103, balance: 1500m);
      db.BankAccounts.Add(account);

      var envelope = TestHelpers.CreateTestEnvelope(id: 104, categoryId: "1", balance: 600m);
      db.Envelopes.Add(envelope);

      var details = new List<TransactionDetail>
      {
        TestHelpers.CreateTestTransactionDetail(
          transactionId: 103,
          lineId: 1,
          envelopeId: envelope.Id,
          amount: 50m,
          notes: "Already voided transaction")
      };

      var transaction = TestHelpers.CreateTestTransaction(
          id: 103,
          accountId: account.Id,
          vendor: "Test Vendor 4",
          totalAmount: 50m,
          isVoided: true, // Already voided
          details: details);

      db.Transactions.Add(transaction);
      await db.SaveChangesAsync();

      var initialAccountBalance = account.Balance;
      var initialEnvelopeBalance = envelope.Balance;

      // Act
      var command = new VoidTransaction.Command(transaction.Id);
      var response = await Client.PostAsJsonAsync("/Transaction/Void", command);

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);

      var errorResponse = await response.Content.ReadAsStringAsync();
      errorResponse.Should().Contain("already voided");

      // Clear change tracker
      db.ChangeTracker.Clear();

      // Verify balances haven't changed
      var updatedAccount = await db.BankAccounts.FindAsync(account.Id);
      var updatedEnvelope = await db.Envelopes.FindAsync(envelope.Id);

      updatedAccount!.Balance.Should().Be(initialAccountBalance);
      updatedEnvelope!.Balance.Should().Be(initialEnvelopeBalance);
    }
  }

  /// <summary>
  /// Test that attempting to void a non-existent transaction returns 404 NotFound
  /// </summary>
  [Fact]
  public async Task VoidTransaction_Should_Return_NotFound_For_NonExistent_Transaction()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      // Act
      var command = new VoidTransaction.Command(99999);
      var response = await Client.PostAsJsonAsync("/Transaction/Void", command);

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

      var errorResponse = await response.Content.ReadAsStringAsync();
      errorResponse.Should().Contain("not found");
    }
  }
}
