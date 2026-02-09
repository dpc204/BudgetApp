using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Budget.Api.Features.Transactions;
using Budget.DB;
using Budget.Shared.Models;
using Carter;
using Fantum.Mediator;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
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
        BudgetContext db = GetTestDBContext();

        // Create test data
        BankAccount account = TestHelpers.CreateTestAccount(id: 100, balance: 1000m);
        db.BankAccounts.Add(account);

        Envelope envelope = TestHelpers.CreateTestEnvelope(id: 100, categoryId: "1", balance: 500m);
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

        Transaction transaction = TestHelpers.CreateTestTransaction(
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

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var initialAccountBalance = account.Balance;

        // Act
        var command = new VoidTransaction.Command(transaction.Id);

        var handler = new VoidTransaction.Handler(db);

        Result<List<EnvelopeDto>> response = await handler.Handle(command, CancellationToken.None);


        // Assert

        // Verify the response contains the updated envelope data
        List<EnvelopeDto> result = response.Value;
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Id.Should().Be(envelope.Id);
        result[0].Balance.Should().Be(500m); // Restored to original

        // Clear change tracker to force reload from database
        db.ChangeTracker.Clear();

        // Reload entities from database
        BankAccount? updatedAccount = await db.BankAccounts.FindAsync([account.Id], TestContext.Current.CancellationToken);
        Transaction? updatedTransaction = await db.Transactions.FindAsync([transaction.Id], TestContext.Current.CancellationToken);

        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(initialAccountBalance + transaction.TotalAmount);
        updatedAccount.Balance.Should().Be(1000m); // Back to original 1000

        updatedTransaction.Should().NotBeNull();
        updatedTransaction!.IsVoided.Should().BeTrue();
    }

    /// <summary>
    /// Test that voiding a transaction adds the amount back to the Envelope balance
    /// </summary>
    [Fact]
    public async Task VoidTransaction_Should_Reverse_Envelope_Balance()
    {
        // Arrange
        BudgetContext db = GetTestDBContext();

        // Create test data
        BankAccount account = TestHelpers.CreateTestAccount(id: 101, balance: 2000m);
        db.BankAccounts.Add(account);

        Envelope envelope = TestHelpers.CreateTestEnvelope(id: 101, categoryId: "1", balance: 800m);
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

        Transaction transaction = TestHelpers.CreateTestTransaction(
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

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var initialEnvelopeBalance = envelope.Balance;

        // Act
        var command = new VoidTransaction.Command(transaction.Id);
        var handler = new VoidTransaction.Handler(db);
        Result<List<EnvelopeDto>> response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.Value.Should().NotBeNull();
        // Clear change tracker to force reload from database
        db.ChangeTracker.Clear();

        List<EnvelopeDto> result = response.Value;
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Balance.Should().Be(initialEnvelopeBalance + 75m);
        result[0].Balance.Should().Be(800m); // Back to original 800
    }

    /// <summary>
    /// Test that voiding a transaction with multiple envelope details correctly reverses all balances
    /// </summary>
    [Fact]
    public async Task VoidTransaction_Should_Reverse_Multiple_Envelope_Balances()
    {
        // Arrange
        BudgetContext db = GetTestDBContext();

        // Create test data
        BankAccount account = TestHelpers.CreateTestAccount(id: 102, balance: 3000m);
        db.BankAccounts.Add(account);

        Envelope envelope1 = TestHelpers.CreateTestEnvelope(id: 102, categoryId: "1", balance: 1000m);
        db.Envelopes.Add(envelope1);

        Envelope envelope2 = TestHelpers.CreateTestEnvelope(id: 103, categoryId: "1", balance: 500m);
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

        Transaction transaction = TestHelpers.CreateTestTransaction(
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

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var command = new VoidTransaction.Command(transaction.Id);
        var handler = new VoidTransaction.Handler(db);
        Result<List<EnvelopeDto>> response = await handler.Handle(command, CancellationToken.None);


        // Assert

        // Clear change tracker to force reload from database
        db.ChangeTracker.Clear();

        List<EnvelopeDto> result = response.Value;
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        EnvelopeDto? env1Result = result!.FirstOrDefault(e => e.Id == envelope1.Id);
        EnvelopeDto? env2Result = result!.FirstOrDefault(e => e.Id == envelope2.Id);

        env1Result.Should().NotBeNull();
        env1Result!.Balance.Should().Be(1000m); // Back to original

        env2Result.Should().NotBeNull();
        env2Result!.Balance.Should().Be(500m); // Back to original

        // Verify account balance
        BankAccount? updatedAccount = await db.BankAccounts.FindAsync([account.Id], TestContext.Current.CancellationToken);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(3000m); // Back to original
    }

    /// <summary>
    /// Test that attempting to void an already voided transaction returns 409 Conflict
    /// </summary>
    [Fact]
    public async Task VoidTransaction_Should_Return_Conflict_When_Already_Voided()
    {
        // Arrange
        BudgetContext db = GetTestDBContext();


        // Create test data with an already voided transaction
        BankAccount account = TestHelpers.CreateTestAccount(id: 103, balance: 1500m);
        db.BankAccounts.Add(account);

        Envelope envelope = TestHelpers.CreateTestEnvelope(id: 104, categoryId: "1", balance: 600m);
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

        Transaction transaction = TestHelpers.CreateTestTransaction(
          id: 103,
          accountId: account.Id,
          vendor: "Test Vendor 4",
          totalAmount: 50m,
          isVoided: true, // Already voided
          details: details);

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var initialAccountBalance = account.Balance;
        var initialEnvelopeBalance = envelope.Balance;

        // Act
        var command = new VoidTransaction.Command(transaction.Id);
        var handler = new VoidTransaction.Handler(db);
        Result<List<EnvelopeDto>> response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Reasons.Should().ContainSingle()
          .Which.Message.Should().Contain("already voided");

        // Clear change tracker
        db.ChangeTracker.Clear();

        // Verify balances haven't changed
        BankAccount? updatedAccount = await db.BankAccounts.FindAsync([account.Id], TestContext.Current.CancellationToken);
        Envelope? updatedEnvelope = await db.Envelopes.FindAsync([envelope.Id], TestContext.Current.CancellationToken);

        updatedAccount!.Balance.Should().Be(initialAccountBalance);
        updatedEnvelope!.Balance.Should().Be(initialEnvelopeBalance);
    }

    /// <summary>
    /// Test that attempting to void a non-existent transaction returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task VoidTransaction_Should_Return_NotFound_For_NonExistent_Transaction()
    {
        // Arrange
        BudgetContext db = GetTestDBContext();

        // Act
        var command = new VoidTransaction.Command(99999);
        var handler = new VoidTransaction.Handler(db);
        Result<List<EnvelopeDto>> response = await handler.Handle(command, CancellationToken.None);


        // Assert
        response.IsSuccess.Should().BeFalse();
        response.IsSuccess.Should().BeFalse();
        response.Reasons.Should().ContainSingle()
          .Which.Message.Should().Contain("not found");
    }
}

