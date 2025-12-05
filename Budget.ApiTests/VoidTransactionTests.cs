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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Budget.ApiTests;

/// <summary>
/// Tests for the VoidTransaction API endpoint
/// </summary>
public class VoidTransactionTests : IClassFixture<BudgetApiTestFactory>
{
    private readonly BudgetApiTestFactory _factory;

    public VoidTransactionTests(BudgetApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test that voiding a transaction adds the amount back to the BankAccount balance
    /// </summary>
    [Fact]
    public async Task VoidTransaction_Should_Reverse_BankAccount_Balance()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        // Create test data
        var account = new BankAccount
        {
            Id = 100,
            Name = "Test Account",
            Balance = 1000m,
            AccountType = BankAccount.AccountTypes.Checking
        };
        db.BankAccounts.Add(account);

        var envelope = new Envelope
        {
            Id = 100,
            Name = "Test Envelope",
            CategoryId = 1,
            Balance = 500m
        };
        db.Envelopes.Add(envelope);

        var transaction = new Transaction
        {
            Id = 100,
            AccountId = account.Id,
            Date = DateTime.UtcNow,
            Vendor = "Test Vendor",
            TotalAmount = 100m,
            UserId = 1,
            IsVoided = false,
            Details = new List<TransactionDetail>
            {
                new TransactionDetail
                {
                    TransactionId = 100,
                    LineId = 1,
                    EnvelopeId = envelope.Id,
                    Amount = 100m,
                    Notes = "Test transaction"
                }
            }
        };
        db.Transactions.Add(transaction);
        
        // Simulate the balance reduction that would have happened when the transaction was created
        account.Balance -= transaction.TotalAmount; // Balance should be 900
        envelope.Balance -= 100m; // Balance should be 400
        
        await db.SaveChangesAsync();

        var initialAccountBalance = account.Balance;
        var initialEnvelopeBalance = envelope.Balance;

        // Act
        var command = new VoidTransaction.Command(transaction.Id);
        var response = await client.PostAsJsonAsync("/Transaction/Void", command);

        // Assert
        response.EnsureSuccessStatusCode();
        
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

    /// <summary>
    /// Test that voiding a transaction adds the amount back to the Envelope balance
    /// </summary>
    [Fact]
    public async Task VoidTransaction_Should_Reverse_Envelope_Balance()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        // Create test data
        var account = new BankAccount
        {
            Id = 101,
            Name = "Test Account 2",
            Balance = 2000m,
            AccountType = BankAccount.AccountTypes.Checking
        };
        db.BankAccounts.Add(account);

        var envelope = new Envelope
        {
            Id = 101,
            Name = "Test Envelope 2",
            CategoryId = 1,
            Balance = 800m
        };
        db.Envelopes.Add(envelope);

        var transaction = new Transaction
        {
            Id = 101,
            AccountId = account.Id,
            Date = DateTime.UtcNow,
            Vendor = "Test Vendor 2",
            TotalAmount = 75m,
            UserId = 1,
            IsVoided = false,
            Details = new List<TransactionDetail>
            {
                new TransactionDetail
                {
                    TransactionId = 101,
                    LineId = 1,
                    EnvelopeId = envelope.Id,
                    Amount = 75m,
                    Notes = "Test transaction detail"
                }
            }
        };
        db.Transactions.Add(transaction);
        
        // Simulate the balance reduction that would have happened when the transaction was created
        account.Balance -= transaction.TotalAmount;
        envelope.Balance -= 75m; // Balance should be 725
        
        await db.SaveChangesAsync();

        var initialEnvelopeBalance = envelope.Balance;

        // Act
        var command = new VoidTransaction.Command(transaction.Id);
        var response = await client.PostAsJsonAsync("/Transaction/Void", command);

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

    /// <summary>
    /// Test that voiding a transaction with multiple envelope details correctly reverses all balances
    /// </summary>
    [Fact]
    public async Task VoidTransaction_Should_Reverse_Multiple_Envelope_Balances()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        // Create test data
        var account = new BankAccount
        {
            Id = 102,
            Name = "Test Account 3",
            Balance = 3000m,
            AccountType = BankAccount.AccountTypes.Checking
        };
        db.BankAccounts.Add(account);

        var envelope1 = new Envelope
        {
            Id = 102,
            Name = "Test Envelope 3A",
            CategoryId = 1,
            Balance = 1000m
        };
        db.Envelopes.Add(envelope1);

        var envelope2 = new Envelope
        {
            Id = 103,
            Name = "Test Envelope 3B",
            CategoryId = 1,
            Balance = 500m
        };
        db.Envelopes.Add(envelope2);

        var transaction = new Transaction
        {
            Id = 102,
            AccountId = account.Id,
            Date = DateTime.UtcNow,
            Vendor = "Test Vendor 3",
            TotalAmount = 150m,
            UserId = 1,
            IsVoided = false,
            Details = new List<TransactionDetail>
            {
                new TransactionDetail
                {
                    TransactionId = 102,
                    LineId = 1,
                    EnvelopeId = envelope1.Id,
                    Amount = 100m,
                    Notes = "First detail"
                },
                new TransactionDetail
                {
                    TransactionId = 102,
                    LineId = 2,
                    EnvelopeId = envelope2.Id,
                    Amount = 50m,
                    Notes = "Second detail"
                }
            }
        };
        db.Transactions.Add(transaction);
        
        // Simulate the balance reduction that would have happened when the transaction was created
        account.Balance -= transaction.TotalAmount; // 3000 - 150 = 2850
        envelope1.Balance -= 100m; // 1000 - 100 = 900
        envelope2.Balance -= 50m; // 500 - 50 = 450
        
        await db.SaveChangesAsync();

        // Act
        var command = new VoidTransaction.Command(transaction.Id);
        var response = await client.PostAsJsonAsync("/Transaction/Void", command);

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

    /// <summary>
    /// Test that attempting to void an already voided transaction throws an exception
    /// </summary>
    [Fact]
    public async Task VoidTransaction_Should_Not_Allow_Double_Voiding()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        // Create test data with an already voided transaction
        var account = new BankAccount
        {
            Id = 103,
            Name = "Test Account 4",
            Balance = 1500m,
            AccountType = BankAccount.AccountTypes.Checking
        };
        db.BankAccounts.Add(account);

        var envelope = new Envelope
        {
            Id = 104,
            Name = "Test Envelope 4",
            CategoryId = 1,
            Balance = 600m
        };
        db.Envelopes.Add(envelope);

        var transaction = new Transaction
        {
            Id = 103,
            AccountId = account.Id,
            Date = DateTime.UtcNow,
            Vendor = "Test Vendor 4",
            TotalAmount = 50m,
            UserId = 1,
            IsVoided = true, // Already voided
            Details = new List<TransactionDetail>
            {
                new TransactionDetail
                {
                    TransactionId = 103,
                    LineId = 1,
                    EnvelopeId = envelope.Id,
                    Amount = 50m,
                    Notes = "Already voided transaction"
                }
            }
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var initialAccountBalance = account.Balance;
        var initialEnvelopeBalance = envelope.Balance;

        // Act & Assert
        var command = new VoidTransaction.Command(transaction.Id);
        
        // The API should throw an exception for already voided transactions
        // In the test environment, this exception propagates through the TestServer
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await client.PostAsJsonAsync("/Transaction/Void", command);
        });
        
        exception.Message.Should().Contain("already voided");
        
        // Clear change tracker
        db.ChangeTracker.Clear();
        
        // Verify balances haven't changed
        var updatedAccount = await db.BankAccounts.FindAsync(account.Id);
        var updatedEnvelope = await db.Envelopes.FindAsync(envelope.Id);
        
        updatedAccount!.Balance.Should().Be(initialAccountBalance);
        updatedEnvelope!.Balance.Should().Be(initialEnvelopeBalance);
    }

    /// <summary>
    /// Test that attempting to void a non-existent transaction throws an exception
    /// </summary>
    [Fact]
    public async Task VoidTransaction_Should_Fail_For_NonExistent_Transaction()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act & Assert - Try to void a transaction that doesn't exist
        var command = new VoidTransaction.Command(99999);
        
        // The API should throw an exception for non-existent transactions
        // In the test environment, this exception propagates through the TestServer
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await client.PostAsJsonAsync("/Transaction/Void", command);
        });
        
        exception.Message.Should().Contain("not found");
    }
}
