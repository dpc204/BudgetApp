using Budget.Shared.Enums;

namespace Budget.ApiTests.Features.Envelopes.EnvelopeMaint;

/// <summary>
/// Unit tests for the GetAll.Handler class.
/// </summary>
public class HandlerTests
{
  /// <summary>
  /// Creates in-memory database options for testing.
  /// </summary>
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
      .Options;
  }

  /// <summary>
  /// Tests that Handle returns an empty list when the database contains no envelopes.
  /// </summary>
  [Fact]
  public async Task Handle_NoEnvelopes_ReturnsEmptyList()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  /// <summary>
  /// Tests that Handle returns a single envelope when the database contains one envelope.
  /// </summary>
  [Fact]
  public async Task Handle_SingleEnvelope_ReturnsSingleResponse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope {
      Id = 1,
      Name = "Groceries",
      Description = "Monthly grocery budget",
      Balance = 500.50m,
      Budget = 1000.00m,
      CategoryId = "cat1",
      SortOrder = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      FamilyId = 1
    };
    var category = new Category {
      CategoryId = "cat1",
      Name = "Test Category",
      Description = "Test",
      FamilyId = 1,
      SortOrder = 1
    };
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(1);

    var response = result.First();
    response.Id.Should().Be(1);
    response.Name.Should().Be("Groceries");
    response.Description.Should().Be("Monthly grocery budget");
    response.Balance.Should().Be(500.50m);
    response.Budget.Should().Be(1000.00m);
    response.CategoryId.Should().Be("cat1");
    response.SortOrder.Should().Be(1);
    response.EnvelopeType.Should().Be(EnvelopeTypes.Standard);
  }

  /// <summary>
  /// Tests that Handle returns all envelopes when the database contains multiple envelopes.
  /// </summary>
  [Fact]
  public async Task Handle_MultipleEnvelopes_ReturnsAllEnvelopes()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelopes = new List<Envelope>
    {
      new()
      {
        Id = 1,
        Name = "Groceries",
        Description = "Food expenses",
        Balance = 500m,
        Budget = 1000m,
        CategoryId = "cat1",
        SortOrder = 1,
        EnvelopeType = EnvelopeTypes.Standard,
        FamilyId = 1
      },
      new()
      {
        Id = 2,
        Name = "Utilities",
        Description = "Electric and water",
        Balance = 200m,
        Budget = 300m,
        CategoryId = "cat1",
        SortOrder = 2,
        EnvelopeType = EnvelopeTypes.Standard,
        FamilyId = 1
      },
      new()
      {
        Id = 3,
        Name = "Salary",
        Description = "Monthly income",
        Balance = 5000m,
        Budget = 5000m,
        CategoryId = "cat3",
        SortOrder = 3,
        EnvelopeType = EnvelopeTypes.Income,
        FamilyId = 1
      }
    };
    var category = new Category {
      CategoryId = "cat1",
      Name = "Test Category",
      Description = "Test",
      FamilyId = 1,
      SortOrder = 1
    };
    context.Categories.Add(category);
    category = new Category {
      CategoryId = "cat3",
      Name = "Test Category",
      Description = "Test",
      FamilyId = 1,
      SortOrder = 1
    };
    context.Categories.Add(category);

    context.Envelopes.AddRange(envelopes);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(3);
    result.Should().Contain(r => r.Name == "Groceries");
    result.Should().Contain(r => r.Name == "Utilities");
    result.Should().Contain(r => r.Name == "Salary");
  }

  /// <summary>
  /// Tests that Handle correctly handles envelopes with null Budget values.
  /// </summary>
  [Fact]
  public async Task Handle_EnvelopeWithNullBudget_ReturnsResponseWithNullBudget()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope {
      Id = 1,
      Name = "Unbudgeted",
      Description = "No budget set",
      Balance = 100m,
      Budget = null,
      CategoryId = "cat1",
      SortOrder = 1,
      EnvelopeType = EnvelopeTypes.Unassigned,
      FamilyId = 1
    };
    var category = new Category {
      CategoryId = "cat1",
      Name = "Test Category",
      Description = "Test",
      FamilyId = 1,
      SortOrder = 1
    };
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(1);

    var response = result.First();
    response.Budget.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle correctly maps all EnvelopeTypes values.
  /// </summary>
  [Theory]
  [InlineData(EnvelopeTypes.Standard)]
  [InlineData(EnvelopeTypes.Income)]
  [InlineData(EnvelopeTypes.Unassigned)]
  [InlineData(EnvelopeTypes.All)]
  public async Task Handle_DifferentEnvelopeTypes_CorrectlyMapsEnvelopeType(EnvelopeTypes envelopeType)
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope {
      Id = 1,
      Name = "Test Envelope",
      Description = "Test",
      Balance = 100m,
      Budget = 200m,
      CategoryId = "cat1",
      SortOrder = 1,
      EnvelopeType = envelopeType,
      FamilyId = 1
    };
    var category = new Category {
      CategoryId = "cat1",
      Name = "Test Category",
      Description = "Test",
      FamilyId = 1,
      SortOrder = 1
    };
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(1);
    result.First().EnvelopeType.Should().Be(envelopeType);
  }

  /// <summary>
  /// Tests that Handle correctly maps envelopes with boundary decimal values for Balance.
  /// </summary>
  [Theory]
  [InlineData(0)]
  [InlineData(-1000.99)]
  [InlineData(999999999.99)]
  public async Task Handle_BoundaryBalanceValues_CorrectlyMapsBalance(decimal balance)
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope {
      Id = 1,
      Name = "Test",
      Description = "Test",
      Balance = balance,
      Budget = 100m,
      CategoryId = "cat1",
      SortOrder = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      FamilyId = 1
    };

    var category = new Category {
      CategoryId = "cat1",
      Name = "Test Category",
      Description = "Test",
      FamilyId = 1,
      SortOrder = 1
    };
    context.Categories.Add(category);

    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(1);
    result.First().Balance.Should().Be(balance);
  }

  /// <summary>
  /// Tests that Handle correctly handles envelopes with empty string properties.
  /// </summary>
  [Fact]
  public async Task Handle_EmptyCategoryProperty_ReturnsNoEnvelopes()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope {
      Id = 1,
      Name = string.Empty,
      Description = string.Empty,
      Balance = 0m,
      Budget = 0m,
      CategoryId = string.Empty,
      SortOrder = 0,
      EnvelopeType = EnvelopeTypes.Standard,
      FamilyId = 1
    };
    var category = new Category {
      CategoryId = "cat1",
      Name = "Test Category",
      Description = "Test",
      FamilyId = 1,
      SortOrder = 1
    };
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(0);


  }

  /// <summary>
  /// Tests that Handle correctly handles envelopes with special characters in string properties.
  /// </summary>
  [Fact]
  public async Task Handle_SpecialCharactersInStrings_CorrectlyMapsStrings()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope {
      Id = 1,
      Name = "Test & Special <chars> \"quotes\"",
      Description = "Line1\nLine2\tTabbed",
      Balance = 100m,
      Budget = 200m,
      CategoryId = "cat-1_special",
      SortOrder = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      FamilyId = 1
    };
    var category = new Category {
      CategoryId = "cat-1_special",
      Name = "Test Category",
      Description = "Test",
      FamilyId = 1,
      SortOrder = 1
    };
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(1);

    var response = result.First();
    response.Name.Should().Be("Test & Special <chars> \"quotes\"");
    response.Description.Should().Be("Line1\nLine2\tTabbed");
    response.CategoryId.Should().Be("cat-1_special");
  }

  /// <summary>
  /// Tests that Handle correctly handles envelopes with boundary SortOrder values.
  /// </summary>
  [Theory]
  [InlineData(int.MinValue)]
  [InlineData(-1)]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(int.MaxValue)]
  public async Task Handle_BoundarySortOrderValues_CorrectlyMapsSortOrder(int sortOrder)
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope {
      Id = 1,
      Name = "Test",
      Description = "Test",
      Balance = 100m,
      Budget = 200m,
      CategoryId = "cat1",
      EnvelopeType = EnvelopeTypes.Standard,
      FamilyId = 1
    };

    var category = new Category {
      CategoryId = "cat1",
      Name = "Test Category",
      Description = "Test",
      FamilyId = 1,
      SortOrder = sortOrder
    };
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(1);
    result.First().SortOrder.Should().Be(sortOrder);
  }

  /// <summary>
  /// Tests that Handle respects cancellation token when operation is cancelled.
  /// </summary>
  [Fact]
  public async Task Handle_CancelledToken_ThrowsOperationCanceledException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    // Add many envelopes to increase likelihood of cancellation being detected
    for(int i = 1; i <= 100; i++)
    {
      context.Envelopes.Add(new Envelope {
        Id = i,
        Name = $"Envelope {i}",
        Description = $"Description {i}",
        Balance = i * 100m,
        Budget = i * 200m,
        CategoryId = $"cat{i}",
        SortOrder = i,
        EnvelopeType = EnvelopeTypes.Standard,
        FamilyId = 1
      });
    }

    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();
    var cts = new CancellationTokenSource();
    cts.Cancel();

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(async () => await handler.Handle(query, cts.Token)
    );
  }

  /// <summary>
  /// Tests that Handle correctly handles envelopes with very long string values.
  /// </summary>
  [Fact]
  public async Task Handle_VeryLongStrings_CorrectlyMapsLongStrings()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var longString = new string('A', 10000);

    var envelope = new Envelope {
      Id = 1,
      Name = longString,
      Description = longString,
      Balance = 100m,
      Budget = 200m,
      CategoryId = "cat1",
      SortOrder = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      FamilyId = 1
    };
    var category = new Category {
      CategoryId = "cat1",
      Name = "Test Category",
      Description = "Test",
      FamilyId = 1,
      SortOrder = 1
    };
    context.Categories.Add(category);
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Handler(context);
    var query = new Api.Features.Envelopes.EnvelopeMaint.GetAll.Query();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(1);

    var response = result.First();
    response.Name.Should().HaveLength(10000);
    response.Description.Should().HaveLength(10000);
  }
}