using System;
using System.Collections.Generic;
using Budget.DB;

namespace Budget.ApiTests;

/// <summary>
/// Helper methods for creating test data
/// </summary>
public static class TestHelpers
{
    private static int _nextAccountId = 1000;
    private static int _nextEnvelopeId = 1000;
    private static int _nextTransactionId = 1000;
    private static int _nextCategoryId = 1000;

    /// <summary>
    /// Creates a test bank account with default or specified values
    /// </summary>
    public static BankAccount CreateTestAccount(
        int? id = null,
        string? name = null,
        decimal balance = 1000m,
        BankAccount.AccountTypes accountType = BankAccount.AccountTypes.Checking)
    {
        var accountId = id ?? _nextAccountId++;
        return new BankAccount
        {
            Id = accountId,
            Name = name ?? $"Test Account {accountId}",
            Balance = balance,
            AccountType = accountType
        };
    }

    /// <summary>
    /// Creates a test envelope with default or specified values
    /// </summary>
    public static Envelope CreateTestEnvelope(
        int? id = null,
        string? name = null,
        int categoryId = 1,
        decimal balance = 500m,
        decimal? budget = null)
    {
        var envelopeId = id ?? _nextEnvelopeId++;
        return new Envelope
        {
            Id = envelopeId,
            Name = name ?? $"Test Envelope {envelopeId}",
            CategoryId = categoryId,
            Balance = balance,
            Budget = budget
        };
    }

    /// <summary>
    /// Creates a test transaction with specified details
    /// </summary>
    public static Transaction CreateTestTransaction(
        int? id = null,
        int accountId = 1,
        string? vendor = null,
        decimal totalAmount = 0m,
        bool isVoided = false,
        List<TransactionDetail>? details = null)
    {
        var transactionId = id ?? _nextTransactionId++;
        var transaction = new Transaction
        {
            Id = transactionId,
            AccountId = accountId,
            Date = DateTime.UtcNow,
            Vendor = vendor ?? $"Test Vendor {transactionId}",
            TotalAmount = totalAmount,
            UserId = 1,
            IsVoided = isVoided
        };

        if (details != null)
        {
            foreach (var detail in details)
            {
                detail.TransactionId = transactionId;
                transaction.Details.Add(detail);
            }
        }

        return transaction;
    }

    /// <summary>
    /// Creates a test transaction detail
    /// </summary>
    public static TransactionDetail CreateTestTransactionDetail(
        int transactionId = 0,
        int lineId = 1,
        int envelopeId = 1,
        decimal amount = 100m,
        string? notes = null)
    {
        return new TransactionDetail
        {
            TransactionId = transactionId,
            LineId = lineId,
            EnvelopeId = envelopeId,
            Amount = amount,
            Notes = notes ?? "Test transaction detail"
        };
    }

    /// <summary>
    /// Creates a test category with default or specified values
    /// </summary>
    public static Category CreateTestCategory(
        int? id = null,
        string? name = null,
        int sortOrder = 1)
    {
        var categoryId = id ?? _nextCategoryId++;
        return new Category
        {
            Id = categoryId,
            Name = name ?? $"Test Category {categoryId}",
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Resets the ID counters for tests
    /// </summary>
    public static void ResetIdCounters()
    {
        _nextAccountId = 1000;
        _nextEnvelopeId = 1000;
        _nextTransactionId = 1000;
        _nextCategoryId = 1000;
    }
}
