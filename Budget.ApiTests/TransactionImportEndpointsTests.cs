namespace Budget.ApiTests;

/// <summary>
/// Tests for Transaction Import API endpoints
/// </summary>
public class TransactionImportEndpointsTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    => new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
      .Options;


  private static BudgetContext GetTestDBContext()
  {
    return new BudgetContext(CreateInMemoryOptions(), new TestCurrentFamilyService());
  }


  /// <summary>
  /// Test ImportTransactions endpoint - should bulk import transactions to staging table
  /// </summary>
  [Fact]
  public async Task ImportTransactions_Should_Bulk_Import_To_Staging_Table()
  {
    await using BudgetContext context = GetTestDBContext();


    // Arrange
    var transactions = new List<TransactionImportDto>
    {
      new()
      {
        Date = DateTime.Today,
        Vendor = "Test Vendor 1",
        Description = "Test Description 1",
        Amount = 100.50m,
        EnvelopeId = 1,
        EnvelopeName = "Groceries",
        UserId = 1
      },
      new()
      {
        Date = DateTime.Today.AddDays(-1),
        Vendor = "Test Vendor 2",
        Description = "Test Description 2",
        Amount = 50.25m,
        EnvelopeId = 2,
        EnvelopeName = "Gas",
        UserId = 1
      }
    };

    var command = new ImportTransactionsToStaging.Command(transactions);
    var handler = new ImportTransactionsToStaging.Handler(context, new TestCurrentFamilyService());

    // Act
    var response = await handler.Handle(command, CancellationToken.None);

    //  var response = await Client.PostAsJsonAsync("/Transaction/Import", command);

    // Assert
    response.Should().Be(2);

    // Verify data in database

    List<TransactionImport> imports = await context.TransactionImports.ToListAsync(TestContext.Current.CancellationToken);
    imports.Should().HaveCount(2);
    imports[0].Vendor.Should().Be("Test Vendor 1");
    imports[1].Vendor.Should().Be("Test Vendor 2");
  }

  /// <summary>
  /// Test GetTransactionImports endpoint - should retrieve staged imports
  /// </summary>
  [Fact]
  public async Task GetTransactionImports_Should_Return_Staged_Imports()
  {
    await using BudgetContext db = GetTestDBContext();


    // Arrange - insert test data directly


    var import1 = new TransactionImport {
      Date = DateTime.Today.AddDays(-3),
      Vendor = "Test Vendor A",
      Description = "Test Description A",
      Amount = 75.00m,
      EnvelopeId = 1,
      EnvelopeName = "Dining",
      UserId = 1,
      FamilyId = 1,
      ImportedAt = DateTime.UtcNow
    };

    var import2 = new TransactionImport {
      Date = DateTime.Today.AddDays(-2),
      Vendor = "Test Vendor B",
      Description = "Test Description B",
      Amount = 125.50m,
      EnvelopeId = 2,
      EnvelopeName = "Shopping",
      UserId = 1,
      FamilyId = 1,
      ImportedAt = DateTime.UtcNow
    };

    db.TransactionImports.AddRange(import1, import2);
    await db.SaveChangesAsync(TestContext.Current.CancellationToken);


    var query = new GetTransactionImports.Query();
    var handler = new GetTransactionImports.Handler(db);

    // Act
    List<TransactionImportDto> imports = await handler.Handle(query, CancellationToken.None);

    // Assert
    imports.Should().NotBeNull();
    imports.Should().HaveCount(2);
    imports![0].Vendor.Should().Be("Test Vendor B"); // Ordered by date
    imports[1].Vendor.Should().Be("Test Vendor A");

  }

  /// <summary>
  /// Test ClearTransactionImports endpoint - should clear all staged imports
  /// </summary>
  [Fact]
  public async Task ClearTransactionImports_Should_Clear_All_Staged_Imports()
  {
    // Arrange - insert test data
    BudgetContext arrangeDb = GetTestDBContext();

    var import1 = new TransactionImport {
      Date = DateTime.Today,
      Vendor = "Test Vendor X",
      Description = "Test Description X",
      Amount = 50.00m,
      EnvelopeId = 1,
      EnvelopeName = "Test",
      UserId = 1,
      FamilyId = 1,
      ImportedAt = DateTime.UtcNow
    };

    arrangeDb.TransactionImports.Add(import1);
    await arrangeDb.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ClearTransactionImports.Handler(arrangeDb);
    var command = new ClearTransactionImports.Command();


    // Act

    var response = await handler.Handle(command, CancellationToken.None);

    // Assert
    response.Should().Be(1);

    // Verify data is cleared

    List<TransactionImport> imports = await arrangeDb.TransactionImports.ToListAsync(TestContext.Current.CancellationToken);
    imports.Should().BeEmpty();
  }

  /// <summary>
  /// Test duplicate detection - should mark duplicates after import
  /// </summary>
  [Fact]
  public async Task ImportTransactions_Should_Detect_Duplicates()
  {
    // Arrange - Create an existing transaction
    BudgetContext arrangeDb = GetTestDBContext();

    BankAccount account = TestHelpers.CreateTestAccount(id: 300, balance: 1000m);
    arrangeDb.BankAccounts.Add(account);

    Envelope envelope = TestHelpers.CreateTestEnvelope(id: 300, categoryId: "1", balance: 500m);
    arrangeDb.Envelopes.Add(envelope);

    var existingTransaction = new Transaction {
      Date = DateTime.Today,
      Vendor = "Duplicate Vendor",
      TotalAmount = 75.00m,
      AccountId = account.Id,
      UserId = 1,
      FamilyId = 1
    };
    arrangeDb.Transactions.Add(existingTransaction);
    await arrangeDb.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Import new transactions with one duplicate
    var transactions = new List<TransactionImportDto>
    {
      new()
      {
        Date = DateTime.Today,
        Vendor = "Duplicate Vendor",
        Description = "This should be marked as duplicate",
        Amount = 75.00m,
        EnvelopeId = 1,
        EnvelopeName = "Test",
        UserId = 1
      },
      new()
      {
        Date = DateTime.Today,
        Vendor = "New Vendor",
        Description = "This should NOT be marked as duplicate",
        Amount = 50.00m,
        EnvelopeId = 1,
        EnvelopeName = "Test",
        UserId = 1
      }
    };

    var command = new ImportTransactionsToStaging.Command(transactions);
    var handler = new ImportTransactionsToStaging.Handler(arrangeDb, new TestCurrentFamilyService());

    // Act
    var response = await handler.Handle(command, CancellationToken.None);
    // Assert
    List<TransactionImport> imports = await arrangeDb.TransactionImports.ToListAsync(TestContext.Current.CancellationToken);

    imports.Should().HaveCount(2);
    imports.First(i => i.Vendor == "Duplicate Vendor").Duplicate.Should().BeTrue();
    imports.First(i => i.Vendor == "New Vendor").Duplicate.Should().BeFalse();
  }

  /// <summary>
  /// Test UpdateTransactionImport endpoint - should update duplicate flag
  /// </summary>
  [Fact]
  public async Task UpdateTransactionImport_Should_Update_Duplicate_Flag()
  {
    // Arrange
    BudgetContext arrangeDb = GetTestDBContext();

    var import = new TransactionImport {
      Date = DateTime.Today,
      Vendor = "Test Vendor",
      Description = "Test",
      Amount = 100m,
      EnvelopeId = 1,
      EnvelopeName = "Test",
      UserId = 1,
      FamilyId = 1,
      ImportedAt = DateTime.UtcNow,
      Duplicate = false
    };

    arrangeDb.TransactionImports.Add(import);
    await arrangeDb.SaveChangesAsync(TestContext.Current.CancellationToken);
    var importId = import.Id;


    var handler = new UpdateTransactionImport.Handler(arrangeDb);
    var command = new UpdateTransactionImport.Command(importId, true, false);

    // Act - Update to mark as duplicate
    _ = await handler.Handle(command, CancellationToken.None);

    // Assert
    TransactionImport? updatedImport = await arrangeDb.TransactionImports.FindAsync([importId], TestContext.Current.CancellationToken);

    updatedImport.Should().NotBeNull();
    updatedImport!.Duplicate.Should().BeTrue();
    updatedImport.KeepDuplicate.Should().BeFalse();
  }

  private class TestCurrentFamilyService : ICurrentFamilyService
  {
    public int FamilyId { get; set; } = 1;
    public int GetCurrentFamilyId() => FamilyId;
  }
}


