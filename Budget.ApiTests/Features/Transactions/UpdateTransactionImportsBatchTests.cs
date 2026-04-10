namespace Budget.ApiTests.Features.Transactions;
/// <summary>
/// Tests for UpdateTransactionImportsBatch Handler
/// </summary>
public partial class UpdateTransactionImportsBatchTests
{
  /// <summary>
  /// Creates DbContextOptions for an in-memory database
  /// </summary>
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
  }

  /// <summary>
  /// Tests that Handle returns 0 when the Ids list is empty without querying the database
  /// </summary>
  [Fact]
  public async Task Handle_EmptyIdsList_ReturnsZero()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([], false);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(0);
  }

  /// <summary>
  /// Tests that Handle updates a single existing TransactionImport and returns 1
  /// </summary>
  [Fact]
  public async Task Handle_SingleExistingId_UpdatesAndReturnsOne()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var import = new TransactionImport {
      Id = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      Description = "Test Description",
      Amount = 100.00m,
      EnvelopeId = 1,
      EnvelopeName = "Test Envelope",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };
    context.Families.Add(family);
    context.TransactionImports.Add(import);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([1], true);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(1);
    TransactionImport? updatedImport = await context.TransactionImports.FindAsync([1], TestContext.Current.CancellationToken);
    updatedImport.Should().NotBeNull();
    updatedImport!.Duplicate.Should().BeTrue();
  }

  /// <summary>
  /// Tests that Handle updates multiple existing TransactionImports and returns the correct count
  /// </summary>
  [Fact]
  public async Task Handle_MultipleExistingIds_UpdatesAllAndReturnsCount()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var import1 = new TransactionImport {
      Id = 1,
      Date = DateTime.UtcNow,
      Vendor = "Vendor 1",
      Description = "Description 1",
      Amount = 50.00m,
      EnvelopeId = 1,
      EnvelopeName = "Envelope 1",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };
    var import2 = new TransactionImport {
      Id = 2,
      Date = DateTime.UtcNow,
      Vendor = "Vendor 2",
      Description = "Description 2",
      Amount = 75.00m,
      EnvelopeId = 1,
      EnvelopeName = "Envelope 1",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };
    var import3 = new TransactionImport {
      Id = 3,
      Date = DateTime.UtcNow,
      Vendor = "Vendor 3",
      Description = "Description 3",
      Amount = 100.00m,
      EnvelopeId = 1,
      EnvelopeName = "Envelope 1",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };
    context.Families.Add(family);
    context.TransactionImports.AddRange(import1, import2, import3);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([1, 2, 3], true);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(3);
    List<TransactionImport> updatedImports = await context.TransactionImports.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
    updatedImports.Should().HaveCount(3);
    updatedImports.Should().OnlyContain(i => i.Duplicate == true);
  }

  /// <summary>
  /// Tests that Handle returns 0 when none of the provided Ids exist in the database
  /// </summary>
  [Fact]
  public async Task Handle_NonExistingIds_ReturnsZero()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([999, 1000, 1001], true);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(0);
  }

  /// <summary>
  /// Tests that Handle updates only existing TransactionImports when provided a mix of existing and non-existing Ids
  /// </summary>
  [Fact]
  public async Task Handle_MixedExistingAndNonExistingIds_UpdatesOnlyExisting()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var import1 = new TransactionImport {
      Id = 1,
      Date = DateTime.UtcNow,
      Vendor = "Vendor 1",
      Description = "Description 1",
      Amount = 50.00m,
      EnvelopeId = 1,
      EnvelopeName = "Envelope 1",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };
    var import2 = new TransactionImport {
      Id = 2,
      Date = DateTime.UtcNow,
      Vendor = "Vendor 2",
      Description = "Description 2",
      Amount = 75.00m,
      EnvelopeId = 1,
      EnvelopeName = "Envelope 1",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };
    context.Families.Add(family);
    context.TransactionImports.AddRange(import1, import2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([1, 2, 999, 1000], true);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(2);
    TransactionImport? updatedImport1 = await context.TransactionImports.FindAsync([1], TestContext.Current.CancellationToken);
    TransactionImport? updatedImport2 = await context.TransactionImports.FindAsync([2], TestContext.Current.CancellationToken);
    updatedImport1.Should().NotBeNull();
    updatedImport1!.Duplicate.Should().BeTrue();
    updatedImport2.Should().NotBeNull();
    updatedImport2!.Duplicate.Should().BeTrue();
  }

  /// <summary>
  /// Tests that Handle correctly sets the Duplicate flag to true when requested
  /// </summary>
  [Fact]
  public async Task Handle_SetsDuplicateToTrue_UpdatesCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var import = new TransactionImport {
      Id = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      Description = "Test Description",
      Amount = 100.00m,
      EnvelopeId = 1,
      EnvelopeName = "Test Envelope",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };
    context.Families.Add(family);
    context.TransactionImports.Add(import);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([1], true);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(1);
    TransactionImport? updatedImport = await context.TransactionImports.FindAsync([1], TestContext.Current.CancellationToken);
    updatedImport.Should().NotBeNull();
    updatedImport!.Duplicate.Should().BeTrue();
  }

  /// <summary>
  /// Tests that Handle correctly sets the Duplicate flag to false when requested
  /// </summary>
  [Fact]
  public async Task Handle_SetsDuplicateToFalse_UpdatesCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var import = new TransactionImport {
      Id = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      Description = "Test Description",
      Amount = 100.00m,
      EnvelopeId = 1,
      EnvelopeName = "Test Envelope",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = true
    };
    context.Families.Add(family);
    context.TransactionImports.Add(import);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([1], false);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(1);
    TransactionImport? updatedImport = await context.TransactionImports.FindAsync([1], TestContext.Current.CancellationToken);
    updatedImport.Should().NotBeNull();
    updatedImport!.Duplicate.Should().BeFalse();
  }

  /// <summary>
  /// Tests that Handle throws OperationCanceledException when cancellation is requested
  /// </summary>
  [Fact]
  public async Task Handle_CancellationRequested_ThrowsOperationCanceledException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var import = new TransactionImport {
      Id = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      Description = "Test Description",
      Amount = 100.00m,
      EnvelopeId = 1,
      EnvelopeName = "Test Envelope",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };
    context.Families.Add(family);
    context.TransactionImports.Add(import);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([1], true);
    var cts = new CancellationTokenSource();
    cts.Cancel();
    // Act
    Func<Task> act = async () => await handler.Handle(command, cts.Token);
    // Assert
    await act.Should().ThrowAsync<OperationCanceledException>();
  }

  /// <summary>
  /// Tests that Handle correctly updates records with boundary values for Id (int.MaxValue)
  /// </summary>
  [Fact]
  public async Task Handle_IdWithMaxValue_UpdatesCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var import = new TransactionImport {
      Id = int.MaxValue,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      Description = "Test Description",
      Amount = 100.00m,
      EnvelopeId = 1,
      EnvelopeName = "Test Envelope",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };
    context.Families.Add(family);
    context.TransactionImports.Add(import);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([int.MaxValue], true);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(1);
    TransactionImport? updatedImport = await context.TransactionImports.FindAsync([int.MaxValue], TestContext.Current.CancellationToken);
    updatedImport.Should().NotBeNull();
    updatedImport!.Duplicate.Should().BeTrue();
  }

  /// <summary>
  /// Tests that Handle correctly handles negative Id values
  /// </summary>
  [Fact]
  public async Task Handle_NegativeId_ReturnsZero()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([-1, -100, -999], true);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(0);
  }

  /// <summary>
  /// Tests that Handle correctly processes a large batch of Ids
  /// </summary>
  [Fact]
  public async Task Handle_LargeBatchOfIds_UpdatesAllCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    context.Families.Add(family);
    var ids = new List<int>();
    for(int i = 1; i <= 100; i++)
    {
      var import = new TransactionImport {
        Id = i,
        Date = DateTime.UtcNow,
        Vendor = $"Vendor {i}",
        Description = $"Description {i}",
        Amount = i * 10.00m,
        EnvelopeId = 1,
        EnvelopeName = "Test Envelope",
        UserId = 1,
        FamilyId = 1,
        Family = family,
        ImportedAt = DateTime.UtcNow,
        Duplicate = false
      };
      context.TransactionImports.Add(import);
      ids.Add(i);
    }

    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command(ids, true);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(100);
    List<TransactionImport> updatedImports = await context.TransactionImports.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
    updatedImports.Should().HaveCount(100);
    updatedImports.Should().OnlyContain(i => i.Duplicate == true);
  }

  /// <summary>
  /// Tests that Handle correctly handles duplicate Ids in the request list
  /// </summary>
  [Fact]
  public async Task Handle_DuplicateIdsInList_UpdatesCorrectly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var import = new TransactionImport {
      Id = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      Description = "Test Description",
      Amount = 100.00m,
      EnvelopeId = 1,
      EnvelopeName = "Test Envelope",
      UserId = 1,
      FamilyId = 1,
      Family = family,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };
    context.Families.Add(family);
    context.TransactionImports.Add(import);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new UpdateTransactionImportsBatch.Handler(context);
    var command = new UpdateTransactionImportsBatch.Command([1, 1, 1], true);
    // Act
    int result = await handler.Handle(command, CancellationToken.None);
    // Assert
    result.Should().Be(1);
    TransactionImport? updatedImport = await context.TransactionImports.FindAsync([1], TestContext.Current.CancellationToken);
    updatedImport.Should().NotBeNull();
    updatedImport!.Duplicate.Should().BeTrue();
  }
}
