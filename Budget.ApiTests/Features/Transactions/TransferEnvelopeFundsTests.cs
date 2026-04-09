using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Budget.Api.Features.Transactions;
using Budget.DB;
using Budget.Shared.Enums;
using Budget.Shared.Models;
using Budget.Shared.Services;
using Carter;
using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Budget.ApiTests.Features.Transactions;


/// <summary>
/// Unit tests for the Handler class in the TransferEnvelopeFunds feature.
/// </summary>
public partial class TransferEnvelopeFundsTests
{
    /// <summary>
    /// Creates an in-memory DbContextOptions for testing purposes.
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    /// <summary>
    /// Tests that Handle successfully transfers funds when all data is valid and account exists.
    /// Input: Valid command with positive amount, existing transfer account, existing envelopes, and valid user.
    /// Expected: Returns successful Result with EnvelopeUpdates, transaction is created and saved.
    /// </summary>
    [Fact]
    public async Task Handle_ValidTransfer_ReturnsSuccess()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = "From Envelope",
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = "To Envelope",
            Balance = 500m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Test Transfer", 10, 20, 100m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var savedTransaction = await context.Transactions.FirstOrDefaultAsync();
        savedTransaction.Should().NotBeNull();
        savedTransaction!.Description.Should().Be("Test Transfer");
        savedTransaction.TotalAmount.Should().Be(100m);
        savedTransaction.Vendor.Should().Be("Transfer");

        var updatedFromEnvelope = await context.Envelopes.FindAsync(10);
        updatedFromEnvelope!.Balance.Should().Be(900m);

        var updatedToEnvelope = await context.Envelopes.FindAsync(20);
        updatedToEnvelope!.Balance.Should().Be(600m);
    }

    /// <summary>
    /// Tests that Handle throws ArgumentNullException when no transfer account exists.
    /// Input: Database without a transfer account.
    /// Expected: Throws ArgumentNullException with specific message.
    /// </summary>
    [Fact]
    public async Task Handle_NoTransferAccount_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Test Transfer", 10, 20, 100m);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("No transfer account found. Please create an account with the type 'Transfers' to use this feature.");
    }

    /// <summary>
    /// Tests that Handle creates transaction with envelope ID as name when envelope does not exist.
    /// Input: Valid transfer but envelopes do not exist in database.
    /// Expected: Transaction notes use envelope IDs instead of names, but MoveBalanceDontSave throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Handle_EnvelopeNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        context.BankAccounts.Add(transferAccount);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Test Transfer", 999, 888, 100m);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("One or both envelopes do not exist.");
    }

    /// <summary>
    /// Tests that Handle correctly processes transfer with zero amount.
    /// Input: Command with Amount = 0.
    /// Expected: Returns success, transaction created with zero amount, envelope balances unchanged.
    /// </summary>
    [Fact]
    public async Task Handle_ZeroAmount_ReturnsSuccess()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = "From Envelope",
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = "To Envelope",
            Balance = 500m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Zero Transfer", 10, 20, 0m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedFromEnvelope = await context.Envelopes.FindAsync(10);
        updatedFromEnvelope!.Balance.Should().Be(1000m);

        var updatedToEnvelope = await context.Envelopes.FindAsync(20);
        updatedToEnvelope!.Balance.Should().Be(500m);
    }

    /// <summary>
    /// Tests that Handle correctly processes transfer with negative amount.
    /// Input: Command with negative Amount.
    /// Expected: Returns success, negative amount is transferred (effectively reversing the direction).
    /// </summary>
    [Fact]
    public async Task Handle_NegativeAmount_ReturnsSuccess()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = "From Envelope",
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = "To Envelope",
            Balance = 500m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Negative Transfer", 10, 20, -50m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedFromEnvelope = await context.Envelopes.FindAsync(10);
        updatedFromEnvelope!.Balance.Should().Be(1050m);

        var updatedToEnvelope = await context.Envelopes.FindAsync(20);
        updatedToEnvelope!.Balance.Should().Be(450m);
    }

    /// <summary>
    /// Tests that Handle correctly processes transfer with very large decimal amount.
    /// Input: Command with Amount near decimal.MaxValue.
    /// Expected: Returns success, transaction created with large amount.
    /// </summary>
    [Fact]
    public async Task Handle_LargeAmount_ReturnsSuccess()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = "From Envelope",
            Balance = decimal.MaxValue,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = "To Envelope",
            Balance = 0m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Large Transfer", 10, 20, 999999999999.99m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var savedTransaction = await context.Transactions.FirstOrDefaultAsync();
        savedTransaction!.TotalAmount.Should().Be(999999999999.99m);
    }

    /// <summary>
    /// Tests that Handle correctly processes transfer when FromEnvelopeId equals ToEnvelopeId.
    /// Input: Command with same envelope for from and to.
    /// Expected: Returns success, envelope balance remains unchanged (adds and subtracts same amount).
    /// </summary>
    [Fact]
    public async Task Handle_SameFromAndToEnvelope_ReturnsSuccess()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var envelope = new Envelope
        {
            Id = 10,
            Name = "Same Envelope",
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Same Envelope Transfer", 10, 10, 100m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedEnvelope = await context.Envelopes.FindAsync(10);
        updatedEnvelope!.Balance.Should().Be(1000m);
    }

    /// <summary>
    /// Tests that Handle correctly processes transfer with empty reason string.
    /// Input: Command with empty string for Reason.
    /// Expected: Returns success, transaction created with empty description.
    /// </summary>
    [Fact]
    public async Task Handle_EmptyReason_ReturnsSuccess()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = "From Envelope",
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = "To Envelope",
            Balance = 500m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command(string.Empty, 10, 20, 100m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var savedTransaction = await context.Transactions.FirstOrDefaultAsync();
        savedTransaction!.Description.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle correctly processes transfer with whitespace-only reason string.
    /// Input: Command with whitespace-only string for Reason.
    /// Expected: Returns success, transaction created with whitespace description.
    /// </summary>
    [Fact]
    public async Task Handle_WhitespaceReason_ReturnsSuccess()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = "From Envelope",
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = "To Envelope",
            Balance = 500m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("   ", 10, 20, 100m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var savedTransaction = await context.Transactions.FirstOrDefaultAsync();
        savedTransaction!.Description.Should().Be("   ");
    }

    /// <summary>
    /// Tests that Handle correctly processes transfer with very long reason string.
    /// Input: Command with a very long string for Reason (1000 characters).
    /// Expected: Returns success, transaction created with long description.
    /// </summary>
    [Fact]
    public async Task Handle_VeryLongReason_ReturnsSuccess()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = "From Envelope",
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = "To Envelope",
            Balance = 500m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var longReason = new string('A', 1000);
        var command = new Command(longReason, 10, 20, 100m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var savedTransaction = await context.Transactions.FirstOrDefaultAsync();
        savedTransaction!.Description.Should().Be(longReason);
    }

    /// <summary>
    /// Tests that Handle correctly processes transfer with special characters in reason.
    /// Input: Command with special characters in Reason string.
    /// Expected: Returns success, transaction created with special characters in description.
    /// </summary>
    [Fact]
    public async Task Handle_SpecialCharactersInReason_ReturnsSuccess()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = "From Envelope",
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = "To Envelope",
            Balance = 500m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Test!@#$%^&*()_+-={}[]|:;<>,.?/~`", 10, 20, 100m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var savedTransaction = await context.Transactions.FirstOrDefaultAsync();
        savedTransaction!.Description.Should().Be("Test!@#$%^&*()_+-={}[]|:;<>,.?/~`");
    }

    /// <summary>
    /// Tests that Handle correctly processes transfer with negative envelope IDs.
    /// Input: Command with negative FromEnvelopeId and ToEnvelopeId.
    /// Expected: Throws InvalidOperationException when envelopes don't exist.
    /// </summary>
    [Fact]
    public async Task Handle_NegativeEnvelopeIds_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        context.BankAccounts.Add(transferAccount);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Negative IDs", -1, -2, 100m);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("One or both envelopes do not exist.");
    }

    /// <summary>
    /// Tests that Handle correctly processes transfer with very large envelope IDs.
    /// Input: Command with FromEnvelopeId and ToEnvelopeId near int.MaxValue.
    /// Expected: Throws InvalidOperationException when envelopes don't exist.
    /// </summary>
    [Fact]
    public async Task Handle_VeryLargeEnvelopeIds_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        context.BankAccounts.Add(transferAccount);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Large IDs", int.MaxValue - 1, int.MaxValue, 100m);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("One or both envelopes do not exist.");
    }

    /// <summary>
    /// Tests that Handle correctly creates transaction details with proper notes format.
    /// Input: Valid transfer with named envelopes.
    /// Expected: Transaction details contain proper "Transfer to/from" notes with envelope names.
    /// </summary>
    [Fact]
    public async Task Handle_ValidTransfer_CreatesCorrectTransactionDetails()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = "Savings",
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = "Vacation",
            Balance = 500m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Moving to vacation", 10, 20, 100m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var savedTransaction = await context.Transactions
            .Include(t => t.Details)
            .FirstOrDefaultAsync();

        savedTransaction.Should().NotBeNull();
        savedTransaction!.Details.Should().HaveCount(2);

        var fromDetail = savedTransaction.Details.FirstOrDefault(d => d.LineId == 0);
        fromDetail.Should().NotBeNull();
        fromDetail!.Amount.Should().Be(100m);
        fromDetail.EnvelopeId.Should().Be(10);
        fromDetail.Notes.Should().Be("Transfer to Vacation");

        var toDetail = savedTransaction.Details.FirstOrDefault(d => d.LineId == 1);
        toDetail.Should().NotBeNull();
        toDetail!.Amount.Should().Be(-100m);
        toDetail.EnvelopeId.Should().Be(20);
        toDetail.Notes.Should().Be("Transfer from Savings");
    }

    /// <summary>
    /// Tests that Handle correctly sets transaction metadata.
    /// Input: Valid transfer.
    /// Expected: Transaction has correct Vendor, UserId, and Date fields.
    /// </summary>
    [Fact]
    public async Task Handle_ValidTransfer_SetsCorrectTransactionMetadata()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = "From Envelope",
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = "To Envelope",
            Balance = 500m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 42 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Test Transfer", 10, 20, 100m);
        var beforeTime = DateTime.UtcNow;

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var afterTime = DateTime.UtcNow;
        result.IsSuccess.Should().BeTrue();

        var savedTransaction = await context.Transactions.FirstOrDefaultAsync();
        savedTransaction.Should().NotBeNull();
        savedTransaction!.Vendor.Should().Be("Transfer");
        savedTransaction.UserId.Should().Be(42);
        savedTransaction.AccountId.Should().Be(1);
        savedTransaction.Date.Should().BeOnOrAfter(beforeTime).And.BeOnOrBefore(afterTime);
    }

    /// <summary>
    /// Tests that Handle with zero envelope IDs throws InvalidOperationException.
    /// Input: Command with FromEnvelopeId = 0 and ToEnvelopeId = 0.
    /// Expected: Throws InvalidOperationException when envelopes don't exist.
    /// </summary>
    [Fact]
    public async Task Handle_ZeroEnvelopeIds_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        context.BankAccounts.Add(transferAccount);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Zero IDs", 0, 0, 100m);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("One or both envelopes do not exist.");
    }

    /// <summary>
    /// Tests that Handle uses envelope ID as name when envelope name is null.
    /// Input: Valid transfer with envelope that has null name.
    /// Expected: Transaction notes use envelope ID as string.
    /// </summary>
    [Fact]
    public async Task Handle_EnvelopeWithNullName_UsesIdAsName()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var transferAccount = new BankAccount
        {
            Id = 1,
            AccountType = AccountTypes.Transfers,
            Name = "Transfer Account",
            FamilyId = 1
        };

        var fromEnvelope = new Envelope
        {
            Id = 10,
            Name = string.Empty,
            Balance = 1000m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        var toEnvelope = new Envelope
        {
            Id = 20,
            Name = string.Empty,
            Balance = 500m,
            FamilyId = 1,
            CategoryId = "cat1"
        };

        context.BankAccounts.Add(transferAccount);
        context.Envelopes.AddRange(fromEnvelope, toEnvelope);
        await context.SaveChangesAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(x => x.User).Returns(new UserInfoDto { Id = 1 });

        var handler = new Handler(context, mockUserAndOptions.Object, null!);
        var command = new Command("Test Transfer", 10, 20, 100m);

        // Act
        Result<EnvelopeDeltas> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var savedTransaction = await context.Transactions
            .Include(t => t.Details)
            .FirstOrDefaultAsync();

        savedTransaction.Should().NotBeNull();

        var fromDetail = savedTransaction!.Details.FirstOrDefault(d => d.LineId == 0);
        fromDetail!.Notes.Should().Be("Transfer to ");

        var toDetail = savedTransaction.Details.FirstOrDefault(d => d.LineId == 1);
        toDetail!.Notes.Should().Be("Transfer from ");
    }
}
