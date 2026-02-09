using Budget.Api.Features.Envelopes;
using Budget.Shared.Enums;

namespace Budget.ApiTests.Features.Envelopes;


/// <summary>
/// Unit tests for GetAllEnvelopes.Handler class.
/// </summary>
public class GetAllEnvelopesHandlerTests
{
    /// <summary>
    /// Creates an in-memory database options instance for testing.
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .Options;
    }

    /// <summary>
    /// Tests that Handle returns all envelopes when EnvelopeType is All.
    /// </summary>
    [Fact]
    public async Task Handle_WithEnvelopeTypeAll_ReturnsAllEnvelopes()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var envelope1 = new Envelope
        {
            Id = 1,
            Name = "Envelope 1",
            Balance = 100.50m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var envelope2 = new Envelope
        {
            Id = 2,
            Name = "Envelope 2",
            Balance = 200.75m,
            Budget = 600.00m,
            CategoryId = "cat1",
            SortOrder = 2,
            EnvelopeType = EnvelopeTypes.Income,
            FamilyId = 1
        };

        var envelope3 = new Envelope
        {
            Id = 3,
            Name = "Envelope 3",
            Balance = 300.00m,
            Budget = null,
            CategoryId = "cat1",
            SortOrder = 3,
            EnvelopeType = EnvelopeTypes.Unassigned,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(envelope1, envelope2, envelope3);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Select(r => r.Id).Should().BeEquivalentTo([1, 2, 3]);
    }

    /// <summary>
    /// Tests that Handle returns only standard envelopes when EnvelopeType is Standard.
    /// </summary>
    [Fact]
    public async Task Handle_WithEnvelopeTypeStandard_ReturnsOnlyStandardEnvelopes()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var standardEnvelope = new Envelope
        {
            Id = 1,
            Name = "Standard Envelope",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var incomeEnvelope = new Envelope
        {
            Id = 2,
            Name = "Income Envelope",
            Balance = 200.00m,
            Budget = 600.00m,
            CategoryId = "cat1",
            SortOrder = 2,
            EnvelopeType = EnvelopeTypes.Income,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(standardEnvelope, incomeEnvelope);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.Standard);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        GetAllEnvelopes.Response envelope = result.Single();
        envelope.Id.Should().Be(1);
        envelope.Name.Should().Be("Standard Envelope");
        envelope.EnvelopeType.Should().Be(EnvelopeTypes.Standard);
    }

    /// <summary>
    /// Tests that Handle returns only income envelopes when EnvelopeType is Income.
    /// </summary>
    [Fact]
    public async Task Handle_WithEnvelopeTypeIncome_ReturnsOnlyIncomeEnvelopes()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var standardEnvelope = new Envelope
        {
            Id = 1,
            Name = "Standard Envelope",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var incomeEnvelope = new Envelope
        {
            Id = 2,
            Name = "Income Envelope",
            Balance = 200.00m,
            Budget = 600.00m,
            CategoryId = "cat1",
            SortOrder = 2,
            EnvelopeType = EnvelopeTypes.Income,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(standardEnvelope, incomeEnvelope);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.Income);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        GetAllEnvelopes.Response envelope = result.Single();
        envelope.Id.Should().Be(2);
        envelope.Name.Should().Be("Income Envelope");
        envelope.EnvelopeType.Should().Be(EnvelopeTypes.Income);
    }

    /// <summary>
    /// Tests that Handle returns only unassigned envelopes when EnvelopeType is Unassigned.
    /// </summary>
    [Fact]
    public async Task Handle_WithEnvelopeTypeUnassigned_ReturnsOnlyUnassignedEnvelopes()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var unassignedEnvelope = new Envelope
        {
            Id = 1,
            Name = "Unassigned Envelope",
            Balance = 300.00m,
            Budget = null,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Unassigned,
            FamilyId = 1
        };

        var standardEnvelope = new Envelope
        {
            Id = 2,
            Name = "Standard Envelope",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 2,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(unassignedEnvelope, standardEnvelope);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.Unassigned);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        GetAllEnvelopes.Response envelope = result.Single();
        envelope.Id.Should().Be(1);
        envelope.Name.Should().Be("Unassigned Envelope");
        envelope.EnvelopeType.Should().Be(EnvelopeTypes.Unassigned);
    }

    /// <summary>
    /// Tests that Handle returns empty collection when no envelopes exist.
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsEmptyCollection()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle returns empty collection when no envelopes match the specified type.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoMatchingEnvelopeType_ReturnsEmptyCollection()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var standardEnvelope = new Envelope
        {
            Id = 1,
            Name = "Standard Envelope",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(standardEnvelope);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.Income);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle returns envelopes ordered by SortOrder in ascending order.
    /// </summary>
    [Fact]
    public async Task Handle_WithMultipleEnvelopes_ReturnsOrderedBySortOrder()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var envelope1 = new Envelope
        {
            Id = 1,
            Name = "Third",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 30,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var envelope2 = new Envelope
        {
            Id = 2,
            Name = "First",
            Balance = 200.00m,
            Budget = 600.00m,
            CategoryId = "cat1",
            SortOrder = 10,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var envelope3 = new Envelope
        {
            Id = 3,
            Name = "Second",
            Balance = 300.00m,
            Budget = 700.00m,
            CategoryId = "cat1",
            SortOrder = 20,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(envelope1, envelope2, envelope3);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        List<GetAllEnvelopes.Response> resultList = [.. result];
        resultList[0].Name.Should().Be("First");
        resultList[0].SortOrder.Should().Be(10);
        resultList[1].Name.Should().Be("Second");
        resultList[1].SortOrder.Should().Be(20);
        resultList[2].Name.Should().Be("Third");
        resultList[2].SortOrder.Should().Be(30);
    }

    /// <summary>
    /// Tests that Handle excludes envelopes without matching categories due to inner join.
    /// </summary>
    [Fact]
    public async Task Handle_WithEnvelopeWithoutMatchingCategory_ExcludesEnvelope()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var validEnvelope = new Envelope
        {
            Id = 1,
            Name = "Valid Envelope",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var orphanedEnvelope = new Envelope
        {
            Id = 2,
            Name = "Orphaned Envelope",
            Balance = 200.00m,
            Budget = 600.00m,
            CategoryId = "nonexistent",
            SortOrder = 2,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(validEnvelope, orphanedEnvelope);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        GetAllEnvelopes.Response envelope = result.Single();
        envelope.Id.Should().Be(1);
        envelope.Name.Should().Be("Valid Envelope");
    }

    /// <summary>
    /// Tests that Handle correctly maps all properties to Response including null Budget.
    /// </summary>
    [Fact]
    public async Task Handle_WithNullableBudget_CorrectlyMapsProperties()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var envelopeWithBudget = new Envelope
        {
            Id = 1,
            Name = "With Budget",
            Balance = 100.50m,
            Budget = 500.25m,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var envelopeWithoutBudget = new Envelope
        {
            Id = 2,
            Name = "Without Budget",
            Balance = 200.75m,
            Budget = null,
            CategoryId = "cat1",
            SortOrder = 2,
            EnvelopeType = EnvelopeTypes.Income,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(envelopeWithBudget, envelopeWithoutBudget);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        GetAllEnvelopes.Response withBudget = result.First(r => r.Id == 1);
        withBudget.Name.Should().Be("With Budget");
        withBudget.Balance.Should().Be(100.50m);
        withBudget.Budget.Should().Be(500.25m);
        withBudget.CategoryId.Should().Be("cat1");
        withBudget.SortOrder.Should().Be(1);
        withBudget.EnvelopeType.Should().Be(EnvelopeTypes.Standard);

        GetAllEnvelopes.Response withoutBudget = result.First(r => r.Id == 2);
        withoutBudget.Name.Should().Be("Without Budget");
        withoutBudget.Balance.Should().Be(200.75m);
        withoutBudget.Budget.Should().BeNull();
        withoutBudget.CategoryId.Should().Be("cat1");
        withoutBudget.SortOrder.Should().Be(2);
        withoutBudget.EnvelopeType.Should().Be(EnvelopeTypes.Income);
    }

    /// <summary>
    /// Tests that Handle correctly handles various SortOrder values including negative, zero, and maximum values.
    /// </summary>
    [Theory]
    [InlineData(-100, 0, 100)]
    [InlineData(int.MinValue, 0, int.MaxValue)]
    [InlineData(5, 5, 5)]
    public async Task Handle_WithVariousSortOrderValues_OrdersCorrectly(int sortOrder1, int sortOrder2, int sortOrder3)
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var envelope1 = new Envelope
        {
            Id = 1,
            Name = "Envelope 1",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = sortOrder1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var envelope2 = new Envelope
        {
            Id = 2,
            Name = "Envelope 2",
            Balance = 200.00m,
            Budget = 600.00m,
            CategoryId = "cat1",
            SortOrder = sortOrder2,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var envelope3 = new Envelope
        {
            Id = 3,
            Name = "Envelope 3",
            Balance = 300.00m,
            Budget = 700.00m,
            CategoryId = "cat1",
            SortOrder = sortOrder3,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(envelope1, envelope2, envelope3);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        List<GetAllEnvelopes.Response> resultList = [.. result];

        // Verify order is ascending by SortOrder
        for (int i = 0; i < resultList.Count - 1; i++)
        {
            resultList[i].SortOrder.Should().BeLessThanOrEqualTo(resultList[i + 1].SortOrder);
        }
    }

    /// <summary>
    /// Tests that Handle correctly handles extreme decimal values for Balance and Budget.
    /// </summary>
    [Fact]
    public async Task Handle_WithExtremeDecimalValues_MapsCorrectly()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var envelope = new Envelope
        {
            Id = 1,
            Name = "Extreme Values",
            Balance = decimal.MaxValue,
            Budget = decimal.MinValue,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        GetAllEnvelopes.Response envelope1 = result.Single();
        envelope1.Balance.Should().Be(decimal.MaxValue);
        envelope1.Budget.Should().Be(decimal.MinValue);
    }

    /// <summary>
    /// Tests that Handle correctly handles empty string values for Name and CategoryId.
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyStringProperties_MapsCorrectly()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "", Name = "", FamilyId = 1 };

        var envelope = new Envelope
        {
            Id = 1,
            Name = "",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        GetAllEnvelopes.Response envelope1 = result.Single();
        envelope1.Name.Should().Be("");
        envelope1.CategoryId.Should().Be("");
    }

    /// <summary>
    /// Tests that Handle uses default Query parameter when EnvelopeType is not specified.
    /// </summary>
    [Fact]
    public async Task Handle_WithDefaultQueryParameter_ReturnsAllEnvelopes()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var envelope1 = new Envelope
        {
            Id = 1,
            Name = "Envelope 1",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var envelope2 = new Envelope
        {
            Id = 2,
            Name = "Envelope 2",
            Balance = 200.00m,
            Budget = 600.00m,
            CategoryId = "cat1",
            SortOrder = 2,
            EnvelopeType = EnvelopeTypes.Income,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.AddRange(envelope1, envelope2);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query();

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that Handle correctly handles invalid enum value cast to EnvelopeTypes.
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidEnvelopeTypeValue_ReturnsNoMatches()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var envelope = new Envelope
        {
            Id = 1,
            Name = "Envelope 1",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query((EnvelopeTypes)999);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle respects cancellation token and throws OperationCanceledException when cancelled.
    /// </summary>
    [Fact]
    public async Task Handle_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var envelope = new Envelope
        {
            Id = 1,
            Name = "Envelope 1",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await handler.Handle(query, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Tests that Handle returns envelopes with maximum int value for Id and SortOrder.
    /// </summary>
    [Fact]
    public async Task Handle_WithMaximumIntValues_MapsCorrectly()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };

        var envelope = new Envelope
        {
            Id = int.MaxValue,
            Name = "Max Int Envelope",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = int.MaxValue,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.Add(category);
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        GetAllEnvelopes.Response envelope1 = result.Single();
        envelope1.Id.Should().Be(int.MaxValue);
        envelope1.SortOrder.Should().Be(int.MaxValue);
    }

    /// <summary>
    /// Tests that Handle correctly handles multiple categories with envelopes.
    /// </summary>
    [Fact]
    public async Task Handle_WithMultipleCategories_ReturnsAllEnvelopes()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);

        var family = new Family { Id = 1, Name = "Test Family" };
        var category1 = new Category { CategoryId = "cat1", Name = "Category 1", FamilyId = 1 };
        var category2 = new Category { CategoryId = "cat2", Name = "Category 2", FamilyId = 1 };

        var envelope1 = new Envelope
        {
            Id = 1,
            Name = "Envelope 1",
            Balance = 100.00m,
            Budget = 500.00m,
            CategoryId = "cat1",
            SortOrder = 1,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        var envelope2 = new Envelope
        {
            Id = 2,
            Name = "Envelope 2",
            Balance = 200.00m,
            Budget = 600.00m,
            CategoryId = "cat2",
            SortOrder = 2,
            EnvelopeType = EnvelopeTypes.Standard,
            FamilyId = 1
        };

        context.Families.Add(family);
        context.Categories.AddRange(category1, category2);
        context.Envelopes.AddRange(envelope1, envelope2);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllEnvelopes.Handler(context);
        var query = new GetAllEnvelopes.Query(EnvelopeTypes.All);

        // Act
        IEnumerable<GetAllEnvelopes.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.CategoryId == "cat1");
        result.Should().Contain(r => r.CategoryId == "cat2");
    }
}

