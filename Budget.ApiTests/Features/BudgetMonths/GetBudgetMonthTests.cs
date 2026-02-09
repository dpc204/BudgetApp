using Budget.Api.Features.BudgetMonths;
using Budget.Shared.Enums;
using Fantum.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Budget.ApiTests.Features.BudgetMonths;


/// <summary>
/// Unit tests for the GetBudgetMonth.Endpoint class
/// </summary>
public partial class EndpointTests
{
    /// <summary>
    /// Tests the acctPeriod calculation with various year and month combinations.
    /// Input: Different year and month values
    /// Expected: Correct acctPeriod calculation (year * 100 + month)
    /// </summary>
    [Theory]
    [InlineData(2024, 1, 202401)]
    [InlineData(2024, 12, 202412)]
    [InlineData(2000, 6, 200006)]
    [InlineData(1999, 12, 199912)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 101)]
    public async Task AddRoutes_EndpointHandler_CalculatesAcctPeriodCorrectly(int year, int month, int expectedAcctPeriod)
    {
        // Arrange
        var mockApp = new Mock<IEndpointRouteBuilder>();
        var mockRouteHandlerBuilder = new Mock<RouteHandlerBuilder>();
        var mockSender = new Mock<ISender>();

        Delegate? capturedHandler = null;

        mockApp
            .Setup(x => x.MapGet(It.IsAny<string>(), It.IsAny<Delegate>()))
            .Callback<string, Delegate>((_, handler) => capturedHandler = handler)
            .Returns(mockRouteHandlerBuilder.Object);

        mockRouteHandlerBuilder
            .Setup(x => x.RequireAuthorization(It.IsAny<string[]>()))
            .Returns(mockRouteHandlerBuilder.Object);

        var expectedResult = new List<GetBudgetMonth.Response>();

        mockSender
            .Setup(x => x.Send(It.IsAny<GetBudgetMonth.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var endpoint = new GetBudgetMonth.Endpoint();

        // Act
        endpoint.AddRoutes(mockApp.Object);

        var handlerMethod = capturedHandler!.Method;
        var result = handlerMethod.Invoke(capturedHandler.Target, [mockSender.Object, year, month]);
        var task = result as Task<IResult>;
        await task!;

        // Assert
        mockSender.Verify(x => x.Send(
            It.Is<GetBudgetMonth.Query>(q => q.AcctPeriod == expectedAcctPeriod),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests the endpoint handler with large year values that could cause integer overflow.
    /// Input: year=int.MaxValue/100, month=12
    /// Expected: Query is sent with calculated acctPeriod without overflow
    /// </summary>
    /// <remarks>
    /// This test identifies a potential bug: the calculation year * 100 + month
    /// could overflow if year is too large. For example, if year > 21474836,
    /// the multiplication will overflow.
    /// </remarks>
    [Fact]
    [Trait("Category", "ProductionBugSuspected")]
    public async Task AddRoutes_EndpointHandler_LargeYearValue_MayOverflow()
    {
        // Arrange
        var mockApp = new Mock<IEndpointRouteBuilder>();
        var mockRouteHandlerBuilder = new Mock<RouteHandlerBuilder>();
        var mockSender = new Mock<ISender>();

        Delegate? capturedHandler = null;

        mockApp
            .Setup(x => x.MapGet(It.IsAny<string>(), It.IsAny<Delegate>()))
            .Callback<string, Delegate>((_, handler) => capturedHandler = handler)
            .Returns(mockRouteHandlerBuilder.Object);

        mockRouteHandlerBuilder
            .Setup(x => x.RequireAuthorization(It.IsAny<string[]>()))
            .Returns(mockRouteHandlerBuilder.Object);

        var expectedResult = new List<GetBudgetMonth.Response>();

        mockSender
            .Setup(x => x.Send(It.IsAny<GetBudgetMonth.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var endpoint = new GetBudgetMonth.Endpoint();

        // Using a large year value that will cause overflow
        int largeYear = int.MaxValue / 100;
        int month = 12;

        // Expected calculation with overflow (in unchecked context, which is the default)
        int expectedAcctPeriod = largeYear * 100 + month;

        // Act
        endpoint.AddRoutes(mockApp.Object);

        var handlerMethod = capturedHandler!.Method;
        var result = handlerMethod.Invoke(capturedHandler.Target, [mockSender.Object, largeYear, month]);
        var task = result as Task<IResult>;
        await task!;

        // Assert
        mockSender.Verify(x => x.Send(
            It.Is<GetBudgetMonth.Query>(q => q.AcctPeriod == expectedAcctPeriod),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}


/// <summary>
/// Unit tests for GetBudgetMonth.Handler
/// </summary>
public partial class GetBudgetMonthTests
{
    /// <summary>
    /// Creates in-memory database options with a unique database name
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .Options;
    }

    /// <summary>
    /// Tests that Handle returns complete budget data when envelopes have matching budget month data
    /// Input: Valid AcctPeriod with matching budget data
    /// Expected: All standard envelopes returned with populated budget fields
    /// </summary>
    [Fact]
    public async Task Handle_WithValidAcctPeriodAndMatchingBudgetData_ReturnsCompleteResponse()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = CatTypes.User, FamilyId = 1 };
        var envelope = new Envelope
        {
            Id = 1,
            Name = "Test Envelope",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            Balance = 100.50m,
            FundAmount = 200.75m,
            FamilyId = 1
        };
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 1,
            Budget = 500.00m,
            BudgetDraft = 550.00m,
            IsBudgetLocked = true,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        context.BudgetMonths.Add(budgetMonth);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        GetBudgetMonth.Response response = result.First();
        response.AcctPeriod.Should().Be(202401);
        response.EnvelopeId.Should().Be(1);
        response.EnvelopeName.Should().Be("Test Envelope");
        response.CategoryId.Should().Be("CAT1");
        response.CategoryName.Should().Be("Test Category");
        response.CategoryType.Should().Be(CatTypes.User);
        response.SortOrder.Should().Be(1);
        response.Budget.Should().Be(500.00m);
        response.BudgetDraft.Should().Be(550.00m);
        response.IsBudgetLocked.Should().BeTrue();
        response.FundAmount.Should().Be(200.75m);
        response.Balance.Should().Be(100.50m);
    }

    /// <summary>
    /// Tests that Handle returns envelopes with null budget values when no matching budget data exists
    /// Input: AcctPeriod with no matching budget data
    /// Expected: All standard envelopes returned with null budget fields and IsBudgetLocked false
    /// </summary>
    [Fact]
    public async Task Handle_WithNoMatchingBudgetData_ReturnsEnvelopesWithNullBudgetValues()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = CatTypes.User, FamilyId = 1 };
        var envelope = new Envelope
        {
            Id = 1,
            Name = "Test Envelope",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            Balance = 100.00m,
            FundAmount = 200.00m,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        GetBudgetMonth.Response response = result.First();
        response.AcctPeriod.Should().Be(202401);
        response.EnvelopeId.Should().Be(1);
        response.Budget.Should().BeNull();
        response.BudgetDraft.Should().BeNull();
        response.IsBudgetLocked.Should().BeFalse();
        response.FundAmount.Should().Be(200.00m);
        response.Balance.Should().Be(100.00m);
    }

    /// <summary>
    /// Tests that Handle returns an empty list when no envelopes exist in the database
    /// Input: Empty database
    /// Expected: Empty enumerable
    /// </summary>
    [Fact]
    public async Task Handle_WithNoEnvelopes_ReturnsEmptyList()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        context.Families.Add(family);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle filters out non-standard envelope types
    /// Input: Mix of Standard and non-Standard envelopes
    /// Expected: Only Standard envelopes returned in results
    /// </summary>
    [Theory]
    [InlineData(EnvelopeTypes.Income)]
    [InlineData(EnvelopeTypes.Unassigned)]
    [InlineData(EnvelopeTypes.All)]
    public async Task Handle_WithNonStandardEnvelopes_FiltersOutNonStandard(EnvelopeTypes nonStandardType)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = CatTypes.User, FamilyId = 1 };
        var standardEnvelope = new Envelope
        {
            Id = 1,
            Name = "Standard Envelope",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            FamilyId = 1
        };
        var nonStandardEnvelope = new Envelope
        {
            Id = 2,
            Name = "Non-Standard Envelope",
            CategoryId = "CAT1",
            EnvelopeType = nonStandardType,
            SortOrder = 2,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(standardEnvelope, nonStandardEnvelope);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().EnvelopeName.Should().Be("Standard Envelope");
    }

    /// <summary>
    /// Tests that Handle returns envelopes ordered by SortOrder property
    /// Input: Multiple envelopes with different sort orders
    /// Expected: Results returned in ascending SortOrder
    /// </summary>
    [Fact]
    public async Task Handle_WithMultipleEnvelopes_ReturnsInSortOrder()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = CatTypes.User, FamilyId = 1 };
        var envelope1 = new Envelope
        {
            Id = 1,
            Name = "Envelope C",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 3,
            FamilyId = 1
        };
        var envelope2 = new Envelope
        {
            Id = 2,
            Name = "Envelope A",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            FamilyId = 1
        };
        var envelope3 = new Envelope
        {
            Id = 3,
            Name = "Envelope B",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(envelope1, envelope2, envelope3);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        List<GetBudgetMonth.Response> resultList = [.. result];
        resultList[0].EnvelopeName.Should().Be("Envelope A");
        resultList[0].SortOrder.Should().Be(1);
        resultList[1].EnvelopeName.Should().Be("Envelope B");
        resultList[1].SortOrder.Should().Be(2);
        resultList[2].EnvelopeName.Should().Be("Envelope C");
        resultList[2].SortOrder.Should().Be(3);
    }

    /// <summary>
    /// Tests that Handle returns all envelopes with mixed budget data availability
    /// Input: Multiple envelopes where only some have budget data for the period
    /// Expected: All envelopes returned, budget data populated where available, null otherwise
    /// </summary>
    [Fact]
    public async Task Handle_WithPartialBudgetData_ReturnsMixedResults()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = CatTypes.User, FamilyId = 1 };
        var envelope1 = new Envelope
        {
            Id = 1,
            Name = "With Budget",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            FamilyId = 1
        };
        var envelope2 = new Envelope
        {
            Id = 2,
            Name = "Without Budget",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2,
            FamilyId = 1
        };
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 1,
            Budget = 100.00m,
            BudgetDraft = 110.00m,
            IsBudgetLocked = true,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(envelope1, envelope2);
        context.BudgetMonths.Add(budgetMonth);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        GetBudgetMonth.Response withBudget = result.First(r => r.EnvelopeId == 1);
        withBudget.Budget.Should().Be(100.00m);
        withBudget.BudgetDraft.Should().Be(110.00m);
        withBudget.IsBudgetLocked.Should().BeTrue();

        GetBudgetMonth.Response withoutBudget = result.First(r => r.EnvelopeId == 2);
        withoutBudget.Budget.Should().BeNull();
        withoutBudget.BudgetDraft.Should().BeNull();
        withoutBudget.IsBudgetLocked.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Handle filters budget data correctly by AcctPeriod
    /// Input: Various AcctPeriod values including boundary cases
    /// Expected: Only budget data matching the requested period is returned
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(202401)]
    [InlineData(999999)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public async Task Handle_WithVariousAcctPeriodValues_FiltersCorrectly(int acctPeriod)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = CatTypes.User, FamilyId = 1 };
        var envelope = new Envelope
        {
            Id = 1,
            Name = "Test Envelope",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            FamilyId = 1
        };
        var budgetMonth1 = new BudgetMonth
        {
            AcctPeriod = acctPeriod,
            EnvelopeId = 1,
            Budget = 100.00m,
            FamilyId = 1
        };
        var budgetMonth2 = new BudgetMonth
        {
            AcctPeriod = 999999,
            EnvelopeId = 1,
            Budget = 200.00m,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        context.BudgetMonths.AddRange(budgetMonth1, budgetMonth2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(acctPeriod);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().AcctPeriod.Should().Be(acctPeriod);
        result.First().Budget.Should().Be(100.00m);
    }

    /// <summary>
    /// Tests that Handle correctly handles null values in nullable budget fields
    /// Input: Budget data with null Budget and BudgetDraft values
    /// Expected: Null values preserved in response without conversion
    /// </summary>
    [Fact]
    public async Task Handle_WithNullableBudgetFields_HandlesNullsCorrectly()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = CatTypes.User, FamilyId = 1 };
        var envelope = new Envelope
        {
            Id = 1,
            Name = "Test Envelope",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            FamilyId = 1
        };
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 1,
            Budget = null,
            BudgetDraft = null,
            IsBudgetLocked = false,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        context.BudgetMonths.Add(budgetMonth);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        GetBudgetMonth.Response response = result.First();
        response.Budget.Should().BeNull();
        response.BudgetDraft.Should().BeNull();
        response.IsBudgetLocked.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Handle correctly uses IsBudgetLocked with default value
    /// Input: Budget data with IsBudgetLocked set to true vs no budget data
    /// Expected: IsBudgetLocked from data when available, false when budget data is null
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WithIsBudgetLocked_UsesCorrectValue(bool isLocked)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = CatTypes.User, FamilyId = 1 };
        var envelope = new Envelope
        {
            Id = 1,
            Name = "Test Envelope",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            FamilyId = 1
        };
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 1,
            Budget = 100.00m,
            IsBudgetLocked = isLocked,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        context.BudgetMonths.Add(budgetMonth);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.First().IsBudgetLocked.Should().Be(isLocked);
    }

    /// <summary>
    /// Tests that Handle includes envelope Balance and FundAmount in response
    /// Input: Envelopes with various Balance and FundAmount values including decimals, zero, and negative
    /// Expected: Balance and FundAmount correctly populated in response
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(100.50, 200.75)]
    [InlineData(-50.25, 150.00)]
    [InlineData(999999.99, 0.01)]
    public async Task Handle_WithVariousBalanceAndFundAmount_ReturnsCorrectValues(decimal balance, decimal fundAmount)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = CatTypes.User, FamilyId = 1 };
        var envelope = new Envelope
        {
            Id = 1,
            Name = "Test Envelope",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            Balance = balance,
            FundAmount = fundAmount,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Balance.Should().Be(balance);
        result.First().FundAmount.Should().Be(fundAmount);
    }

    /// <summary>
    /// Tests that Handle correctly includes CategoryType in response
    /// Input: Envelopes with categories of different CategoryType values
    /// Expected: CategoryType correctly populated in response
    /// </summary>
    [Theory]
    [InlineData(CatTypes.User)]
    [InlineData(CatTypes.System)]
    [InlineData(CatTypes.Income)]
    public async Task Handle_WithVariousCategoryTypes_ReturnsCorrectCategoryType(CatTypes categoryType)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = categoryType, FamilyId = 1 };
        var envelope = new Envelope
        {
            Id = 1,
            Name = "Test Envelope",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.First().CategoryType.Should().Be(categoryType);
    }

    /// <summary>
    /// Tests that Handle correctly includes CategoryId and CategoryName from related Category
    /// Input: Envelopes with various category IDs and names
    /// Expected: CategoryId and CategoryName correctly populated from the related Category entity
    /// </summary>
    [Fact]
    public async Task Handle_WithCategoryRelationship_PopulatesCategoryFields()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category1 = new Category { CategoryId = "CAT1", Name = "Category One", CategoryType = CatTypes.User, FamilyId = 1 };
        var category2 = new Category { CategoryId = "CAT2", Name = "Category Two", CategoryType = CatTypes.System, FamilyId = 1 };
        var envelope1 = new Envelope
        {
            Id = 1,
            Name = "Envelope 1",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            FamilyId = 1
        };
        var envelope2 = new Envelope
        {
            Id = 2,
            Name = "Envelope 2",
            CategoryId = "CAT2",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 2,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.AddRange(category1, category2);
        context.Envelopes.AddRange(envelope1, envelope2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        GetBudgetMonth.Response env1 = result.First(r => r.EnvelopeId == 1);
        env1.CategoryId.Should().Be("CAT1");
        env1.CategoryName.Should().Be("Category One");

        GetBudgetMonth.Response env2 = result.First(r => r.EnvelopeId == 2);
        env2.CategoryId.Should().Be("CAT2");
        env2.CategoryName.Should().Be("Category Two");
    }

    /// <summary>
    /// Tests that Handle correctly handles decimal precision for budget values
    /// Input: Budget data with precise decimal values
    /// Expected: Decimal precision preserved in response
    /// </summary>
    [Fact]
    public async Task Handle_WithDecimalPrecision_PreservesAccuracy()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "CAT1", Name = "Test Category", CategoryType = CatTypes.User, FamilyId = 1 };
        var envelope = new Envelope
        {
            Id = 1,
            Name = "Test Envelope",
            CategoryId = "CAT1",
            EnvelopeType = EnvelopeTypes.Standard,
            SortOrder = 1,
            Balance = 123.45m,
            FundAmount = 678.90m,
            FamilyId = 1
        };
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = 202401,
            EnvelopeId = 1,
            Budget = 1234.56m,
            BudgetDraft = 7890.12m,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        context.BudgetMonths.Add(budgetMonth);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBudgetMonth.Handler(context);
        var query = new GetBudgetMonth.Query(202401);

        // Act
        IEnumerable<GetBudgetMonth.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        GetBudgetMonth.Response response = result.First();
        response.Budget.Should().Be(1234.56m);
        response.BudgetDraft.Should().Be(7890.12m);
        response.Balance.Should().Be(123.45m);
        response.FundAmount.Should().Be(678.90m);
    }
}