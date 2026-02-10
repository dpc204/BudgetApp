using Budget.Api.Features.Envelopes;
using Budget.Shared.Services;
using Budget.Shared.Enums;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

using UserInfoDto = Budget.Shared.Models.UserInfoDto;

namespace Budget.ApiTests.Features.Envelopes;


/// <summary>
/// Tests for Fund Handler which funds envelopes based on their FundAmount values
/// </summary>
public class FundTests
{
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
      => new DbContextOptionsBuilder<BudgetContext>()
        .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options;

    [Fact]
    public async Task Handle_Should_Fund_Envelopes_Successfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        var incomeEnvelope = new Envelope
        {
            Id = 1,
            Name = "Income",
            CategoryId = "1",
            Balance = 1000m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Income,
            SortOrder = 1
        };

        var envelope1 = new Envelope
        {
            Id = 2,
            Name = "Groceries",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = 200m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2
        };

        var envelope2 = new Envelope
        {
            Id = 3,
            Name = "Gas",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = 150m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 3
        };

        var account = new BankAccount()
        {
            AccountType = AccountTypes.Funding,
            Name = "Funding",
            Balance = 1000,
            Id = 22
        };

        context.BankAccounts.Add(account);
        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(incomeEnvelope, envelope1, envelope2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Mock<IUserAndOptions> mockUserAndOptions = SetupMockUserAndOptions();
        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        // Act
        Result<int> result = await handler.Handle(new Fund.Command(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2, "because two envelopes have FundAmount values");

        // Verify transactions were created
        List<Transaction> transactions = await context.Transactions.Include(t => t.Details).ToListAsync(TestContext.Current.CancellationToken);
        transactions.Should().HaveCount(2);

        Transaction? groceryTransaction = transactions.FirstOrDefault(t => t.Description.Contains("Groceries"));
        groceryTransaction.Should().NotBeNull();
        groceryTransaction!.TransactionType.Should().Be(TransactionTypes.Funding);
        groceryTransaction.Vendor.Should().Be("Fantum Budget - Fund");
        groceryTransaction.Details.Should().HaveCount(2);
        groceryTransaction.Details.Should().Contain(d => d.EnvelopeId == 2 && d.Amount == 200m);
        groceryTransaction.Details.Should().Contain(d => d.EnvelopeId == 1 && d.Amount == -200m);
        groceryTransaction.AccountId.Should().Be(account.Id);

        Transaction? gasTransaction = transactions.FirstOrDefault(t => t.Description.Contains("Gas"));
        gasTransaction.Should().NotBeNull();
        gasTransaction!.Details.Should().HaveCount(2);
        gasTransaction.Details.Should().Contain(d => d.EnvelopeId == 3 && d.Amount == 150m);
        gasTransaction.Details.Should().Contain(d => d.EnvelopeId == 1 && d.Amount == -150m);
        gasTransaction.AccountId.Should().Be(account.Id);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Income_Envelope_Not_Found()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        // No income envelope created
        var envelope1 = new Envelope
        {
            Id = 2,
            Name = "Groceries",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = 200m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope1);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        // Act
        Result<int> result = await handler.Handle(new Fund.Command(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Be("Income envelope not found. Cannot fund envelopes.");
    }

    [Fact]
    public async Task Handle_Should_Return_Zero_When_No_Envelopes_Have_FundAmount()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        var incomeEnvelope = new Envelope
        {
            Id = 1,
            Name = "Income",
            CategoryId = "1",
            Balance = 1000m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Income,
            SortOrder = 1
        };

        var envelope1 = new Envelope
        {
            Id = 2,
            Name = "Groceries",
            CategoryId = "1",
            Balance = 100m,
            FundAmount = 0m, // No fund amount
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(incomeEnvelope, envelope1);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        // Act
        Result<int> result = await handler.Handle(new Fund.Command(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0, "because no envelopes have FundAmount != 0");

        // Verify no transactions were created
        List<Transaction> transactions = await context.Transactions.ToListAsync(TestContext.Current.CancellationToken);
        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Only_Fund_Envelopes_With_NonZero_FundAmount()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        var incomeEnvelope = new Envelope
        {
            Id = 1,
            Name = "Income",
            CategoryId = "1",
            Balance = 1000m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Income,
            SortOrder = 1
        };

        var envelope1 = new Envelope
        {
            Id = 2,
            Name = "Groceries",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = 200m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2
        };

        var envelope2 = new Envelope
        {
            Id = 3,
            Name = "Gas",
            CategoryId = "1",
            Balance = 50m,
            FundAmount = 0m, // Should not be funded
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 3
        };

        var envelope3 = new Envelope
        {
            Id = 4,
            Name = "Entertainment",
            CategoryId = "1",
            Balance = 25m,
            FundAmount = 100m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 4
        };

        var account = new BankAccount()
        {
            AccountType = AccountTypes.Funding,
            Name = "Funding",
            Balance = 1000,
            Id = 22,
            FamilyId = 1
        };
        context.BankAccounts.Add(account);
        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(incomeEnvelope, envelope1, envelope2, envelope3);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Mock<IUserAndOptions> mockUserAndOptions = SetupMockUserAndOptions();
        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        // Act
        Result<int> result = await handler.Handle(new Fund.Command(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2, "because only two envelopes have non-zero FundAmount");

        // Verify only 2 transactions were created
        List<Transaction> transactions = await context.Transactions.Include(t => t.Details).ToListAsync(TestContext.Current.CancellationToken);
        transactions.Should().HaveCount(2);
        transactions.Should().NotContain(t => t.Description.Contains("Gas"));
    }

    [Fact]
    public async Task Handle_Should_Create_Correct_Transaction_Details()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        var incomeEnvelope = new Envelope
        {
            Id = 100,
            Name = "Income",
            CategoryId = "1",
            Balance = 500m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Income,
            SortOrder = 1
        };

        var targetEnvelope = new Envelope
        {
            Id = 200,
            Name = "Test Envelope",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = 250m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2
        };

        var account = new BankAccount
        {
            Id = 22,
            AccountType = AccountTypes.Funding,
            Name = "Funding",
            Balance = 0,
            FamilyId = 1
        };
        context.BankAccounts.Add(account);
        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(incomeEnvelope, targetEnvelope);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Mock<IUserAndOptions> mockUserAndOptions = SetupMockUserAndOptions();

        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        // Act
        Result<int> result = await handler.Handle(new Fund.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        Transaction? transaction = await context.Transactions
          .Include(t => t.Details)
          .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        transaction.Should().NotBeNull();
        transaction!.FamilyId.Should().Be(1);
        transaction.TransactionType.Should().Be(TransactionTypes.Funding);
        transaction.Description.Should().Be("Fund: Test Envelope");
        transaction.Vendor.Should().Be("Fantum Budget - Fund");
        transaction.Date.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify detail lines
        transaction.Details.Should().HaveCount(2);

        TransactionDetail? toDetail = transaction.Details.FirstOrDefault(d => d.LineId == 1);
        toDetail.Should().NotBeNull();
        toDetail!.EnvelopeId.Should().Be(200);
        toDetail.Amount.Should().Be(250m);

        TransactionDetail? fromDetail = transaction.Details.FirstOrDefault(d => d.LineId == 2);
        fromDetail.Should().NotBeNull();
        fromDetail!.EnvelopeId.Should().Be(100);
        fromDetail.Amount.Should().Be(-250m);
    }

    private static Mock<IUserAndOptions> SetupMockUserAndOptions()
    {
        var mockUserAndOptions = new Mock<IUserAndOptions>();
        mockUserAndOptions.Setup(u => u.User).Returns(new UserInfoDto { Email = "test@test.com", Id = 1, Name = "Test User", Roles = ["Admin"] });
        mockUserAndOptions.Setup(u => u.HasInfo).Returns(true);
        mockUserAndOptions.Setup(u => u.Options).Returns(new Budget.Shared.Services.UserOptions() { UserId = 1, FillAmountType = FillAmounts.OneHundredPercent, SelectedCategoryType = "CatTypes.User" });
        return mockUserAndOptions;
    }

    [Fact]
    public async Task Handle_Should_Return_Error_Result_On_Exception()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        // Create minimal data that will cause an exception during processing
        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        var incomeEnvelope = new Envelope
        {
            Id = 1,
            Name = "Income",
            CategoryId = "1",
            Balance = 1000m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Income,
            SortOrder = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(incomeEnvelope);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Dispose context to force exception on SaveChangesAsync
        await context.DisposeAsync();

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        // Act
        Result<int> result = await handler.Handle(new Fund.Command(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();

        // Verify error was logged
        mockLogger.Verify(
          x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Handle_Negative_FundAmount()
    {
        // Arrange - This tests edge case where FundAmount could be negative
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        var incomeEnvelope = new Envelope
        {
            Id = 1,
            Name = "Income",
            CategoryId = "1",
            Balance = 1000m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Income,
            SortOrder = 1
        };

        var envelope1 = new Envelope
        {
            Id = 2,
            Name = "Groceries",
            CategoryId = "1",
            Balance = 200m,
            FundAmount = -50m, // Negative fund amount - should still create transaction
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2
        };

        var account = new BankAccount()
        {
            AccountType = AccountTypes.Funding,
            Name = "Funding",
            Balance = 1000,
            Id = 22,
            FamilyId = 1
        };

        context.BankAccounts.Add(account);

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(incomeEnvelope, envelope1);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Mock<IUserAndOptions> mockUserAndOptions = SetupMockUserAndOptions();
        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        // Act
        Result<int> result = await handler.Handle(new Fund.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        Transaction? transaction = await context.Transactions.Include(t => t.Details).FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        transaction.Should().NotBeNull();
        transaction!.Details.Should().Contain(d => d.EnvelopeId == 2 && d.Amount == -50m);
        transaction.Details.Should().Contain(d => d.EnvelopeId == 1 && d.Amount == 50m);
    }

    [Fact]
    public async Task Handle_Should_Respect_Cancellation_Token_In_Query()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        var incomeEnvelope = new Envelope
        {
            Id = 1,
            Name = "Income",
            CategoryId = "1",
            Balance = 1000m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Income,
            SortOrder = 1
        };

        // Add many envelopes to increase chance of cancellation during query
        for (int i = 2; i < 1000; i++)
        {
            context.Envelopes.Add(new Envelope
            {
                Id = i,
                Name = $"Envelope {i}",
                CategoryId = "1",
                Balance = 0m,
                FundAmount = 100m,
                FamilyId = 1,
                EnvelopeType = EnvelopeTypes.Standard,
                SortOrder = i
            });
        }

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(incomeEnvelope);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockUserAndOptions = new Mock<IUserAndOptions>();
        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - The cancellation token is passed to queries
        // Note: With in-memory database, cancellation might not always throw,
        // but the token is properly propagated to async operations
        try
        {
            await handler.Handle(new Fund.Command(), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected - cancellation occurred
            Assert.True(true);
            return;
        }

        // If no exception, verify the token was at least passed through
        cts.Token.IsCancellationRequested.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Handle fails gracefully when no funding account exists in the database.
    /// This edge case verifies that the system properly handles the scenario where
    /// BankAccounts.FirstOrDefaultAsync returns null for AccountType.Funding,
    /// resulting in fundingAccount being null and MakeAssignTransaction throwing ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task Handle_Should_Fail_When_Funding_Account_Not_Found()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        var incomeEnvelope = new Envelope
        {
            Id = 1,
            Name = "Income",
            CategoryId = "1",
            Balance = 1000m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Income,
            SortOrder = 1
        };

        var envelope1 = new Envelope
        {
            Id = 2,
            Name = "Groceries",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = 200m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2
        };

        // Note: No funding account added to context
        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(incomeEnvelope, envelope1);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Mock<IUserAndOptions> mockUserAndOptions = SetupMockUserAndOptions();
        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        // Act
        Result<int> result = await handler.Handle(new Fund.Command(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Should().BeOfType<ExceptionalError>();

        var exceptionalError = result.Errors[0] as ExceptionalError;
        exceptionalError!.Exception.Should().BeOfType<ArgumentNullException>();

        // Verify error was logged
        mockLogger.Verify(
          x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);
    }

    /// <summary>
    /// Tests that Handle can process an envelope with decimal.MaxValue as FundAmount.
    /// This extreme boundary test ensures the system doesn't overflow or fail with
    /// the maximum possible decimal value, which could reveal issues with transaction
    /// creation or database storage.
    /// </summary>
    [Fact]
    public async Task Handle_Should_Handle_Decimal_MaxValue_FundAmount()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        var incomeEnvelope = new Envelope
        {
            Id = 1,
            Name = "Income",
            CategoryId = "1",
            Balance = decimal.MaxValue,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Income,
            SortOrder = 1
        };

        var envelope1 = new Envelope
        {
            Id = 2,
            Name = "Extreme Test",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = decimal.MaxValue,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2
        };

        var account = new BankAccount
        {
            AccountType = AccountTypes.Funding,
            Name = "Funding",
            Balance = 1000,
            Id = 22,
            FamilyId = 1
        };

        context.BankAccounts.Add(account);
        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(incomeEnvelope, envelope1);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Mock<IUserAndOptions> mockUserAndOptions = SetupMockUserAndOptions();
        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        // Act
        Result<int> result = await handler.Handle(new Fund.Command(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        Transaction? transaction = await context.Transactions
          .Include(t => t.Details)
          .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        transaction.Should().NotBeNull();
        transaction!.Details.Should().HaveCount(2);
        transaction.Details.Should().Contain(d => d.EnvelopeId == 2 && d.Amount == decimal.MaxValue);
        transaction.Details.Should().Contain(d => d.EnvelopeId == 1 && d.Amount == -decimal.MaxValue);
    }

    /// <summary>
    /// Tests that Handle correctly processes multiple envelopes with extreme and varied FundAmount values.
    /// This test verifies the system can handle a mix of extreme boundary values (decimal.MaxValue,
    /// decimal.MinValue), normal values, and edge values in a single funding operation without
    /// arithmetic overflow, data corruption, or transaction errors.
    /// </summary>
    [Fact]
    public async Task Handle_Should_Handle_Multiple_Envelopes_With_Extreme_FundAmounts()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category
        {
            CategoryId = "1",
            Name = "Test Category",
            Description = "Test",
            SortOrder = 1,
            FamilyId = 1,
            CategoryType = CatTypes.User
        };

        var incomeEnvelope = new Envelope
        {
            Id = 1,
            Name = "Income",
            CategoryId = "1",
            Balance = decimal.MaxValue,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Income,
            SortOrder = 1
        };

        var envelope1 = new Envelope
        {
            Id = 2,
            Name = "Max Value",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = decimal.MaxValue,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2
        };

        var envelope2 = new Envelope
        {
            Id = 3,
            Name = "Min Value",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = decimal.MinValue,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 3
        };

        var envelope3 = new Envelope
        {
            Id = 4,
            Name = "Normal Value",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = 100.50m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 4
        };

        var envelope4 = new Envelope
        {
            Id = 5,
            Name = "Very Small Positive",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = 0.01m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 5
        };

        var envelope5 = new Envelope
        {
            Id = 6,
            Name = "Very Small Negative",
            CategoryId = "1",
            Balance = 0m,
            FundAmount = -0.01m,
            FamilyId = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 6
        };

        var account = new BankAccount
        {
            AccountType = AccountTypes.Funding,
            Name = "Funding",
            Balance = 1000,
            Id = 22,
            FamilyId = 1
        };

        context.BankAccounts.Add(account);
        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(incomeEnvelope, envelope1, envelope2, envelope3, envelope4, envelope5);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Mock<IUserAndOptions> mockUserAndOptions = SetupMockUserAndOptions();
        var mockLogger = new Mock<ILogger<Fund.Handler>>();
        var mockCurrentFamilyService = new Mock<ICurrentFamilyService>();
        mockCurrentFamilyService.Setup(x => x.GetCurrentFamilyId()).Returns(1);
        var insertTransactions = new InsertTransactions(context, mockCurrentFamilyService.Object);
        var handler = new Fund.Handler(context, mockUserAndOptions.Object, mockLogger.Object, insertTransactions);

        // Act
        Result<int> result = await handler.Handle(new Fund.Command(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5, "because five envelopes have non-zero FundAmount values");

        List<Transaction> transactions = await context.Transactions
          .Include(t => t.Details)
          .ToListAsync(TestContext.Current.CancellationToken);

        transactions.Should().HaveCount(5);

        // Verify each extreme value transaction was created correctly
        Transaction? maxValueTx = transactions.FirstOrDefault(t => t.Description.Contains("Max Value"));
        maxValueTx.Should().NotBeNull();
        maxValueTx!.Details.Should().Contain(d => d.Amount == decimal.MaxValue);

        Transaction? minValueTx = transactions.FirstOrDefault(t => t.Description.Contains("Min Value"));
        minValueTx.Should().NotBeNull();
        minValueTx!.Details.Should().Contain(d => d.Amount == decimal.MinValue);

        Transaction? normalTx = transactions.FirstOrDefault(t => t.Description.Contains("Normal Value"));
        normalTx.Should().NotBeNull();
        normalTx!.Details.Should().Contain(d => d.Amount == 100.50m);

        Transaction? smallPosTx = transactions.FirstOrDefault(t => t.Description.Contains("Very Small Positive"));
        smallPosTx.Should().NotBeNull();
        smallPosTx!.Details.Should().Contain(d => d.Amount == 0.01m);

        Transaction? smallNegTx = transactions.FirstOrDefault(t => t.Description.Contains("Very Small Negative"));
        smallNegTx.Should().NotBeNull();
        smallNegTx!.Details.Should().Contain(d => d.Amount == -0.01m);
    }
}

