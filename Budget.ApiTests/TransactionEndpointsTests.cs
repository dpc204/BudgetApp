using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Budget.Api.Features.Accounts.AccountMaint;
using Budget.Api.Features.Transactions;
using Budget.Shared.Models;
using Budget.DB;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Budget.ApiTests;

/// <summary>
/// Tests for Transaction API endpoints
/// </summary>
public class TransactionEndpointsTests : IClassFixture<BudgetApiTestFactory>
{
    private readonly BudgetApiTestFactory _factory;

    public TransactionEndpointsTests(BudgetApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test AddNewTransaction endpoint - should create a new transaction and update balances
    /// </summary>
    [Fact]
    public async Task AddNewTransaction_Should_Create_Transaction_And_Update_Balances()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var account = TestHelpers.CreateTestAccount(id: 200, balance: 1000m);
        db.BankAccounts.Add(account);

        var envelope = TestHelpers.CreateTestEnvelope(id: 200, categoryId: 1, balance: 500m);
        db.Envelopes.Add(envelope);

        await db.SaveChangesAsync();

        var transactionDetail = new OneTransactionDetail
        {
            AccountId = account.Id,
            Date = DateTime.UtcNow,
            Vendor = "Test Vendor",
            UserId = 1,
            Details = new List<TransactionDto>
            {
                new TransactionDto
                {
                    EnvelopeId = envelope.Id,
                    Amount = 100m,
                    Description = "Test purchase"
                }
            }
        };

        var command = new AddNewTransaction.Command(transactionDetail);

        // Act
        var response = await client.PostAsJsonAsync("/Transaction/Insert", command);

        // Assert
        response.EnsureSuccessStatusCode();
        
        db.ChangeTracker.Clear();
        var updatedAccount = await db.BankAccounts.FindAsync(account.Id);
        var updatedEnvelope = await db.Envelopes.FindAsync(envelope.Id);

        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Should().Be(900m); // 1000 - 100
        
        updatedEnvelope.Should().NotBeNull();
        updatedEnvelope!.Balance.Should().Be(400m); // 500 - 100
    }

    /// <summary>
    /// Test GetUnassigned endpoint - should return transactions assigned to the Unallocated envelope
    /// </summary>
    [Fact]
    public async Task GetUnassigned_Should_Return_Unallocated_Transactions()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var account = TestHelpers.CreateTestAccount(id: 201, balance: 1000m);
        db.BankAccounts.Add(account);

        // Note: Unallocated envelope with ID -1 should already exist from seed data
        // We don't need to create it

        var details = new List<TransactionDetail>
        {
            TestHelpers.CreateTestTransactionDetail(
                transactionId: 201,
                lineId: 1,
                envelopeId: -1, // Unallocated
                amount: 50m,
                notes: "Unassigned transaction")
        };

        var transaction = TestHelpers.CreateTestTransaction(
            id: 201,
            accountId: account.Id,
            vendor: "Test Vendor",
            totalAmount: 50m,
            details: details);

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync("/transactions/unassigned");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<GetUnassigned.Response>>();
        
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        var ourTransaction = result!.FirstOrDefault(r => r.TransactionId == 201);
        ourTransaction.Should().NotBeNull();
        ourTransaction!.envelopeId.Should().Be(-1);
    }

    /// <summary>
    /// Test GetByEnvelopeId endpoint - should return transactions for a specific envelope
    /// </summary>
    [Fact]
    public async Task GetByEnvelopeId_Should_Return_Transactions_For_Envelope()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var account = TestHelpers.CreateTestAccount(id: 202, balance: 1000m);
        db.BankAccounts.Add(account);

        var envelope = TestHelpers.CreateTestEnvelope(id: 202, categoryId: 1, balance: 500m);
        db.Envelopes.Add(envelope);

        var details = new List<TransactionDetail>
        {
            TestHelpers.CreateTestTransactionDetail(
                transactionId: 202,
                lineId: 1,
                envelopeId: envelope.Id,
                amount: 75m,
                notes: "Test transaction for envelope")
        };

        var transaction = TestHelpers.CreateTestTransaction(
            id: 202,
            accountId: account.Id,
            vendor: "Test Vendor",
            totalAmount: 75m,
            details: details);

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/transactions/{envelope.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<GetByEnvelopeId.Response>>();
        
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].TransactionId.Should().Be(202);
        result[0].Amount.Should().Be(75m);
    }

    /// <summary>
    /// Test GetOneTransactionDetail endpoint - should return transaction details
    /// </summary>
    [Fact]
    public async Task GetOneTransactionDetail_Should_Return_Transaction_Details()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var account = TestHelpers.CreateTestAccount(id: 203, balance: 1000m);
        db.BankAccounts.Add(account);

        var envelope = TestHelpers.CreateTestEnvelope(id: 203, categoryId: 1, balance: 500m);
        db.Envelopes.Add(envelope);

        var details = new List<TransactionDetail>
        {
            TestHelpers.CreateTestTransactionDetail(
                transactionId: 203,
                lineId: 1,
                envelopeId: envelope.Id,
                amount: 60m,
                notes: "Test detail")
        };

        var transaction = TestHelpers.CreateTestTransaction(
            id: 203,
            accountId: account.Id,
            vendor: "Test Vendor",
            totalAmount: 60m,
            details: details);

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/transactions/detail/{transaction.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetOneTransactionDetail.Response>();
        
        result.Should().NotBeNull();
        result!.Id.Should().Be(203);
        result.Vendor.Should().Be("Test Vendor");
        result.Details.Should().HaveCount(1);
    }

    /// <summary>
    /// Test AssignTransaction endpoint - should reassign transaction detail to different envelope
    /// </summary>
    [Fact]
    public async Task AssignTransaction_Should_Reassign_Transaction_Detail()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var account = TestHelpers.CreateTestAccount(id: 204, balance: 1000m);
        db.BankAccounts.Add(account);

        var envelope1 = TestHelpers.CreateTestEnvelope(id: 204, name: "Envelope 1", categoryId: 1, balance: 500m);
        db.Envelopes.Add(envelope1);

        var envelope2 = TestHelpers.CreateTestEnvelope(id: 205, name: "Envelope 2", categoryId: 1, balance: 300m);
        db.Envelopes.Add(envelope2);

        var details = new List<TransactionDetail>
        {
            TestHelpers.CreateTestTransactionDetail(
                transactionId: 204,
                lineId: 1,
                envelopeId: envelope1.Id,
                amount: 40m,
                notes: "Original note")
        };

        var transaction = TestHelpers.CreateTestTransaction(
            id: 204,
            accountId: account.Id,
            vendor: "Test Vendor",
            totalAmount: 40m,
            details: details);

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var command = new AssignTransaction.Command(
            TransactionId: 204,
            LineId: 1,
            EnvelopeId: envelope2.Id,
            Description: "Updated note");

        // Act
        var response = await client.PutAsJsonAsync("/transactions/assign", command);

        // Assert
        response.EnsureSuccessStatusCode();
        
        db.ChangeTracker.Clear();
        var updatedDetail = await db.TransactionDetails
            .FirstOrDefaultAsync(td => td.TransactionId == 204 && td.LineId == 1);
        
        updatedDetail.Should().NotBeNull();
        updatedDetail!.EnvelopeId.Should().Be(envelope2.Id);
        updatedDetail.Notes.Should().Be("Updated note");
    }

    /// <summary>
    /// Test UpdateTransaction endpoint - should update transaction and recalculate balances
    /// </summary>
    [Fact]
    public async Task UpdateTransaction_Should_Update_Transaction_And_Recalculate_Balances()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var account = TestHelpers.CreateTestAccount(id: 2050, balance: 1000m);
        db.BankAccounts.Add(account);

        var envelope = TestHelpers.CreateTestEnvelope(id: 2060, categoryId: 1, balance: 500m);
        db.Envelopes.Add(envelope);

        var details = new List<TransactionDetail>
        {
            TestHelpers.CreateTestTransactionDetail(
                transactionId: 2050,
                lineId: 1,
                envelopeId: envelope.Id,
                amount: 100m,
                notes: "Original transaction")
        };

        var transaction = TestHelpers.CreateTestTransaction(
            id: 2050,
            accountId: account.Id,
            vendor: "Original Vendor",
            totalAmount: 100m,
            details: details);

        db.Transactions.Add(transaction);
        
        // Simulate balance reduction that would have happened when transaction was created
        account.Balance -= 100m; // 1000 - 100 = 900
        envelope.Balance -= 100m; // 500 - 100 = 400
        
        await db.SaveChangesAsync();
        
        // Clear change tracker to ensure we're working with fresh data
        db.ChangeTracker.Clear();

        var updatedTransaction = new OneTransactionDetail
        {
            Id = 2050,
            AccountId = account.Id,
            Date = DateTime.UtcNow,
            Vendor = "Updated Vendor",
            UserId = 1,
            Details = new List<TransactionDto>
            {
                new TransactionDto
                {
                    EnvelopeId = envelope.Id,
                    Amount = 150m, // Changed amount
                    Description = "Updated transaction"
                }
            }
        };

        var command = new UpdateTransaction.Command(updatedTransaction);

        // Act
        var response = await client.PutAsJsonAsync("/Transaction/Update", command);

        // Assert
        response.EnsureSuccessStatusCode();
        
        db.ChangeTracker.Clear();
        var updatedTrans = await db.Transactions
            .Include(t => t.Details)
            .FirstOrDefaultAsync(t => t.Id == 2050);
        var updatedAcct = await db.BankAccounts.FindAsync(account.Id);
        var updatedEnv = await db.Envelopes.FindAsync(envelope.Id);

        updatedTrans.Should().NotBeNull();
        updatedTrans!.Vendor.Should().Be("Updated Vendor");
        updatedTrans.TotalAmount.Should().Be(150m);
        updatedTrans.Details.Should().HaveCount(1);
        updatedTrans.Details.First().Amount.Should().Be(150m);
        
        // Account: started at 1000, reduced by 100 to 900, then restored 100 back to 1000, 
        // then reduced by NEW total (150), final = 850
        updatedAcct!.Balance.Should().Be(850m);
        
        // Envelope: started at 500, reduced by 100 to 400, then restored 100 back to 500,
        // then reduced by NEW amount (150), final = 350
        updatedEnv!.Balance.Should().Be(350m);
    }
}
