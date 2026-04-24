using Budget.Shared.Enums;

namespace Budget.ApiTests;

/// <summary>
/// Tests for Transaction API endpoints
/// </summary>
public class TransactionEndpointsTests : IntegrationTestBase
{
  //private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  //  => new DbContextOptionsBuilder<BudgetContext>()
  //    .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
  //    .Options;

  [Fact]
  public async Task AddNewTransaction_Should_Create_Transaction_And_Update_Balances()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var account = new BankAccount { Id = 200, Name = "Test Account", Balance = 1000m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope {
      Id = 200,
      Name = "Test Envelope",
      CategoryId = "1",
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Users.Add(user);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var transactionDetail = new OneTransactionDetail {
      AccountId = account.Id,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      UserId = 1,
      Details =
      [
        new TransactionDetailDto
        {
          TransactionId = 0,
          EnvelopeId = envelope.Id,
          Amount = -100m,
          Notes = "Test purchase"
          // LineId will be assigned by backend
        }
      ]
    };


    var mockFamilyService = new TestCurrentFamilyService(1);
    var inserter = new InsertTransactions(context, mockFamilyService);
    var handler = new AddNewTransaction.Handler(inserter);
    var command = new AddNewTransaction.Command(transactionDetail);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();

    context.ChangeTracker.Clear();
    BankAccount? updatedAccount = await context.BankAccounts.FindAsync([account.Id], TestContext.Current.CancellationToken);
    Envelope? updatedEnvelope = await context.Envelopes.FindAsync([envelope.Id], TestContext.Current.CancellationToken);

    updatedAccount.Should().NotBeNull();
    updatedAccount!.Balance.Should().Be(900m); // 1000 - 100

    updatedEnvelope.Should().NotBeNull();
    updatedEnvelope!.Balance.Should().Be(400m); // 500 - 100
  }

  [Fact]
  public async Task GetUnassigned_Should_Return_Unallocated_Transactions()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "-2", Name = "Unassigned", Description = "Unassigned", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.System };
    var account = new BankAccount { Id = 201, Name = "Test Account", Balance = 1000m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var unassignedEnvelope = new Envelope {
      Id = -2,
      Name = "Unassigned",
      CategoryId = "-2",
      Balance = 0m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Unassigned,
      SortOrder = 999
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(unassignedEnvelope);
    context.Users.Add(user);

    var transaction = new Transaction {
      Id = 300,
      AccountId = 201,
      Vendor = "Unassigned Vendor",
      Date = DateTime.UtcNow,
      TotalAmount = 50m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail {
      TransactionId = 300,
      LineId = 1,
      EnvelopeId = -2,
      Amount = 50m,
      Notes = "Unassigned"
    };

    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetUnassigned.Handler(context);

    // Act
    Result<IEnumerable<GetUnassigned.Response>> result = await handler.Handle(new GetUnassigned.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    var resultList = result.Value.ToList();
    resultList.Should().HaveCountGreaterThanOrEqualTo(1);
    resultList.Should().Contain(t => t.TransactionId == 300);
  }

  [Fact]
  public async Task GetByEnvelopeId_Should_Return_Transactions_For_Envelope()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var account = new BankAccount { Id = 202, Name = "Test Account", Balance = 1000m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope {
      Id = 202,
      Name = "Groceries",
      CategoryId = "1",
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Users.Add(user);

    var transaction = new Transaction {
      Id = 400,
      AccountId = 202,
      Vendor = "Store",
      Date = DateTime.UtcNow,
      TotalAmount = 75m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail {
      TransactionId = 400,
      LineId = 1,
      EnvelopeId = 202,
      Amount = 75m,
      Notes = "Groceries"
    };

    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetByEnvelopeId.Handler(context);

    // Act
    IEnumerable<GetByEnvelopeId.Response> result = await handler.Handle(new GetByEnvelopeId.Query(202), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var resultList = result.ToList();
    resultList.Should().HaveCount(1);
    resultList[0].TransactionId.Should().Be(400);
    resultList[0].Amount.Should().Be(75m);
  }

  [Fact]
  public async Task GetOneTransactionDetail_Should_Return_Transaction_Details()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var account = new BankAccount { Id = 203, Name = "Test Account", Balance = 1000m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope {
      Id = 203,
      Name = "Test Envelope",
      CategoryId = "1",
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Users.Add(user);

    var transaction = new Transaction {
      Id = 500,
      AccountId = 203,
      Vendor = "Test Vendor",
      Date = DateTime.UtcNow,
      TotalAmount = 100m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail {
      TransactionId = 500,
      LineId = 1,
      EnvelopeId = 203,
      Amount = 100m,
      Notes = "Test"
    };

    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetOneTransactionDetail.Handler(context);

    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(new GetOneTransactionDetail.Query(500), CancellationToken.None);

    // Assert

    result.Should().NotBeNull();
    if(result != null)
    {
      result?.Id.Should().Be(500);
      result?.AccountId.Should().Be(203);
      result?.Vendor.Should().Be("Test Vendor");
      result?.Details.Should().HaveCount(1);
      result?.Details[0].Amount.Should().Be(100m);
    }
  }

  [Fact]
  public async Task AssignTransaction_Should_Reassign_Transaction_Detail()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var unallocatedCategory = new Category { CategoryId = "-1", Name = "UnAllocated", Description = "UnAllocated", SortOrder = 999, FamilyId = 1, CategoryType = CatTypes.System };
    var account = new BankAccount { Id = 204, Name = "Test Account", Balance = 1000m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope {
      Id = 204,
      Name = "Groceries",
      CategoryId = "1",
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var unallocatedEnvelope = new Envelope {
      Id = -1,
      Name = "UnAllocated",
      CategoryId = "-1",
      Balance = 50m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Income,
      SortOrder = 999
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.AddRange(category, unallocatedCategory);
    context.BankAccounts.Add(account);
    context.Envelopes.AddRange(envelope, unallocatedEnvelope);
    context.Users.Add(user);

    var transaction = new Transaction {
      Id = 600,
      AccountId = 204,
      Vendor = "Store",
      Date = DateTime.UtcNow,
      TotalAmount = 50m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail {
      TransactionId = 600,
      LineId = 1,
      EnvelopeId = -1,
      Amount = 50m,
      Notes = "Unassigned"
    };

    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new AssignTransaction.Handler(context, new MoveEnvelopeBalance(), NullLogger<AssignTransaction.Handler>.Instance);
    var command = new AssignTransaction.Command(600, 1, 204, "Updated Vendor", "Reassigned", "notes");

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeTrue();

    context.ChangeTracker.Clear();
    TransactionDetail? updatedDetail = await context.TransactionDetails.FindAsync([600, 1], TestContext.Current.CancellationToken);
    updatedDetail.Should().NotBeNull();
    updatedDetail!.EnvelopeId.Should().Be(204);
  }

  [Fact]
  public async Task UpdateTransaction_Should_Update_Transaction_And_Recalculate_Balances()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category { CategoryId = "1", Name = "Test", Description = "Test", SortOrder = 1, FamilyId = 1, CategoryType = CatTypes.User };
    var account = new BankAccount { Id = 205, Name = "Test Account", Balance = 900m, AccountType = AccountTypes.Checking, FamilyId = 1 };
    var envelope = new Envelope {
      Id = 205,
      Name = "Test Envelope",
      CategoryId = "1",
      Balance = -400m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.Add(category);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Users.Add(user);

    var transaction = new Transaction {
      Id = 700,
      AccountId = 205,
      Vendor = "Original Vendor",
      Date = DateTime.UtcNow,
      TotalAmount = -100m,
      UserId = 1,
      FamilyId = 1
    };
    var detail = new TransactionDetail {
      TransactionId = 700,
      LineId = 1,
      EnvelopeId = 205,
      Amount = -100m,
      Notes = "Original"
    };

    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateTransaction.Handler(context);
    var updatedTransaction = new OneTransactionDetail {
      Id = 700,
      AccountId = 205,
      Date = DateTime.UtcNow,
      Vendor = "Updated Vendor",
      UserId = 1,
      Details =
      [
        new TransactionDetailDto
        {
          TransactionId = 700,
          LineId = 1,
          EnvelopeId = 205,
          Amount = -150m,
          Notes = "Updated"
        }
      ]
    };
    var command = new UpdateTransaction.Command(updatedTransaction);

    // Act
    Result<List<EnvelopeUpdate>> result = await handler.Handle(command, CancellationToken.None);

    // Assert
    var testResult = result.Value.FirstOrDefault();
    testResult?.EnvelopeDelta.Should().Be(-50);
    testResult?.EnvelopeId.Should().Be(205);

    context.ChangeTracker.Clear();
    Transaction? updatedTx = await context.Transactions.FindAsync([700], TestContext.Current.CancellationToken);
    updatedTx.Should().NotBeNull();
    updatedTx!.Vendor.Should().Be("Updated Vendor");
    updatedTx.TotalAmount.Should().Be(-150m);
  }

  [Fact]
  public async Task UpdateTransaction_Should_Return_NotFound_For_NonExistent_Transaction()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var handler = new UpdateTransaction.Handler(context);
    var transaction = new OneTransactionDetail {
      Id = 99999,
      AccountId = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test",
      UserId = 1,
      Details = []
    };
    var command = new UpdateTransaction.Command(transaction);

    // Act
    Result<List<EnvelopeUpdate>> result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailed.Should().Be(true);
  }

  [Fact]
  public async Task ClearHiddenUnassigned_WithHiddenUnassignedTransactions_ReturnsCorrectCount()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var unassignedCategory = new Category {
      CategoryId = "-2",
      Name = "Unassigned",
      Description = "Unassigned",
      SortOrder = 999,
      FamilyId = 1,
      CategoryType = CatTypes.System
    };
    var regularCategory = new Category {
      CategoryId = "1",
      Name = "Groceries",
      Description = "Groceries",
      SortOrder = 1,
      FamilyId = 1,
      CategoryType = CatTypes.User
    };
    var account = new BankAccount {
      Id = 800,
      Name = "Test Account",
      Balance = 1000m,
      AccountType = AccountTypes.Checking,
      FamilyId = 1
    };
    var unassignedEnvelope = new Envelope {
      Id = -2,
      Name = "Unassigned",
      CategoryId = "-2",
      Balance = 0m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Unassigned,
      SortOrder = 999
    };
    var regularEnvelope = new Envelope {
      Id = 801,
      Name = "Groceries",
      CategoryId = "1",
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.AddRange(unassignedCategory, regularCategory);
    context.BankAccounts.Add(account);
    context.Envelopes.AddRange(unassignedEnvelope, regularEnvelope);
    context.Users.Add(user);

    // Create 3 hidden unassigned transactions
    var hiddenUnassigned1 = new Transaction {
      Id = 801,
      AccountId = 800,
      Vendor = "Hidden Vendor 1",
      Date = DateTime.UtcNow,
      TotalAmount = 50m,
      UserId = 1,
      FamilyId = 1,
      TransactionHiddenFromAssign = true
    };
    var detail1 = new TransactionDetail {
      TransactionId = 801,
      LineId = 1,
      EnvelopeId = -2,
      Amount = 50m,
      Notes = "Hidden Unassigned 1"
    };

    var hiddenUnassigned2 = new Transaction {
      Id = 802,
      AccountId = 800,
      Vendor = "Hidden Vendor 2",
      Date = DateTime.UtcNow,
      TotalAmount = 75m,
      UserId = 1,
      FamilyId = 1,
      TransactionHiddenFromAssign = true
    };
    var detail2 = new TransactionDetail {
      TransactionId = 802,
      LineId = 1,
      EnvelopeId = -2,
      Amount = 75m,
      Notes = "Hidden Unassigned 2"
    };

    var hiddenUnassigned3 = new Transaction {
      Id = 803,
      AccountId = 800,
      Vendor = "Hidden Vendor 3",
      Date = DateTime.UtcNow,
      TotalAmount = 100m,
      UserId = 1,
      FamilyId = 1,
      TransactionHiddenFromAssign = true
    };
    var detail3 = new TransactionDetail {
      TransactionId = 803,
      LineId = 1,
      EnvelopeId = -2,
      Amount = 100m,
      Notes = "Hidden Unassigned 3"
    };

    // Create a visible unassigned transaction (should not be affected)
    var visibleUnassigned = new Transaction {
      Id = 804,
      AccountId = 800,
      Vendor = "Visible Vendor",
      Date = DateTime.UtcNow,
      TotalAmount = 25m,
      UserId = 1,
      FamilyId = 1,
      TransactionHiddenFromAssign = false
    };
    var detail4 = new TransactionDetail {
      TransactionId = 804,
      LineId = 1,
      EnvelopeId = -2,
      Amount = 25m,
      Notes = "Visible Unassigned"
    };

    // Create a hidden assigned transaction (should not be affected)
    var hiddenAssigned = new Transaction {
      Id = 805,
      AccountId = 800,
      Vendor = "Hidden Assigned",
      Date = DateTime.UtcNow,
      TotalAmount = 30m,
      UserId = 1,
      FamilyId = 1,
      TransactionHiddenFromAssign = true
    };
    var detail5 = new TransactionDetail {
      TransactionId = 805,
      LineId = 1,
      EnvelopeId = 801,
      Amount = 30m,
      Notes = "Hidden but Assigned"
    };

    context.Transactions.AddRange(hiddenUnassigned1, hiddenUnassigned2, hiddenUnassigned3, visibleUnassigned, hiddenAssigned);
    context.TransactionDetails.AddRange(detail1, detail2, detail3, detail4, detail5);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ClearHiddenUnassigned.Handler(context);
    var command = new ClearHiddenUnassigned.Command();

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().Be(3);

    context.ChangeTracker.Clear();
    var transaction1 = await context.Transactions.FindAsync([801], TestContext.Current.CancellationToken);
    var transaction2 = await context.Transactions.FindAsync([802], TestContext.Current.CancellationToken);
    var transaction3 = await context.Transactions.FindAsync([803], TestContext.Current.CancellationToken);
    var transaction4 = await context.Transactions.FindAsync([804], TestContext.Current.CancellationToken);
    var transaction5 = await context.Transactions.FindAsync([805], TestContext.Current.CancellationToken);

    transaction1.Should().NotBeNull();
    transaction1!.TransactionHiddenFromAssign.Should().BeFalse();

    transaction2.Should().NotBeNull();
    transaction2!.TransactionHiddenFromAssign.Should().BeFalse();

    transaction3.Should().NotBeNull();
    transaction3!.TransactionHiddenFromAssign.Should().BeFalse();

    transaction4.Should().NotBeNull();
    transaction4!.TransactionHiddenFromAssign.Should().BeFalse();

    transaction5.Should().NotBeNull();
    transaction5!.TransactionHiddenFromAssign.Should().BeTrue();
  }

  [Fact]
  public async Task ClearHiddenUnassigned_WithNoHiddenTransactions_ReturnsZero()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var unassignedCategory = new Category {
      CategoryId = "-2",
      Name = "Unassigned",
      Description = "Unassigned",
      SortOrder = 999,
      FamilyId = 1,
      CategoryType = CatTypes.System
    };
    var account = new BankAccount {
      Id = 810,
      Name = "Test Account",
      Balance = 1000m,
      AccountType = AccountTypes.Checking,
      FamilyId = 1
    };
    var unassignedEnvelope = new Envelope {
      Id = -2,
      Name = "Unassigned",
      CategoryId = "-2",
      Balance = 0m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Unassigned,
      SortOrder = 999
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.Add(unassignedCategory);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(unassignedEnvelope);
    context.Users.Add(user);

    // Create visible unassigned transactions
    var visibleUnassigned1 = new Transaction {
      Id = 811,
      AccountId = 810,
      Vendor = "Visible Vendor 1",
      Date = DateTime.UtcNow,
      TotalAmount = 50m,
      UserId = 1,
      FamilyId = 1,
      TransactionHiddenFromAssign = false
    };
    var detail1 = new TransactionDetail {
      TransactionId = 811,
      LineId = 1,
      EnvelopeId = -2,
      Amount = 50m,
      Notes = "Visible Unassigned"
    };

    var visibleUnassigned2 = new Transaction {
      Id = 812,
      AccountId = 810,
      Vendor = "Visible Vendor 2",
      Date = DateTime.UtcNow,
      TotalAmount = 75m,
      UserId = 1,
      FamilyId = 1,
      TransactionHiddenFromAssign = false
    };
    var detail2 = new TransactionDetail {
      TransactionId = 812,
      LineId = 1,
      EnvelopeId = -2,
      Amount = 75m,
      Notes = "Visible Unassigned 2"
    };

    context.Transactions.AddRange(visibleUnassigned1, visibleUnassigned2);
    context.TransactionDetails.AddRange(detail1, detail2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ClearHiddenUnassigned.Handler(context);
    var command = new ClearHiddenUnassigned.Command();

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().Be(0);

    context.ChangeTracker.Clear();
    var transaction1 = await context.Transactions.FindAsync([811], TestContext.Current.CancellationToken);
    var transaction2 = await context.Transactions.FindAsync([812], TestContext.Current.CancellationToken);

    transaction1.Should().NotBeNull();
    transaction1!.TransactionHiddenFromAssign.Should().BeFalse();

    transaction2.Should().NotBeNull();
    transaction2!.TransactionHiddenFromAssign.Should().BeFalse();
  }

  [Fact]
  public async Task ClearHiddenUnassigned_WithHiddenAssignedTransactions_DoesNotClearThem()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var unassignedCategory = new Category {
      CategoryId = "-2",
      Name = "Unassigned",
      Description = "Unassigned",
      SortOrder = 999,
      FamilyId = 1,
      CategoryType = CatTypes.System
    };
    var regularCategory = new Category {
      CategoryId = "1",
      Name = "Groceries",
      Description = "Groceries",
      SortOrder = 1,
      FamilyId = 1,
      CategoryType = CatTypes.User
    };
    var account = new BankAccount {
      Id = 820,
      Name = "Test Account",
      Balance = 1000m,
      AccountType = AccountTypes.Checking,
      FamilyId = 1
    };
    var unassignedEnvelope = new Envelope {
      Id = -2,
      Name = "Unassigned",
      CategoryId = "-2",
      Balance = 0m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Unassigned,
      SortOrder = 999
    };
    var regularEnvelope = new Envelope {
      Id = 821,
      Name = "Groceries",
      CategoryId = "1",
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.AddRange(unassignedCategory, regularCategory);
    context.BankAccounts.Add(account);
    context.Envelopes.AddRange(unassignedEnvelope, regularEnvelope);
    context.Users.Add(user);

    // Create hidden transactions assigned to regular envelope
    var hiddenAssigned1 = new Transaction {
      Id = 822,
      AccountId = 820,
      Vendor = "Hidden Assigned 1",
      Date = DateTime.UtcNow,
      TotalAmount = 50m,
      UserId = 1,
      FamilyId = 1,
      TransactionHiddenFromAssign = true
    };
    var detail1 = new TransactionDetail {
      TransactionId = 822,
      LineId = 1,
      EnvelopeId = 821,
      Amount = 50m,
      Notes = "Hidden but Assigned 1"
    };

    var hiddenAssigned2 = new Transaction {
      Id = 823,
      AccountId = 820,
      Vendor = "Hidden Assigned 2",
      Date = DateTime.UtcNow,
      TotalAmount = 75m,
      UserId = 1,
      FamilyId = 1,
      TransactionHiddenFromAssign = true
    };
    var detail2 = new TransactionDetail {
      TransactionId = 823,
      LineId = 1,
      EnvelopeId = 821,
      Amount = 75m,
      Notes = "Hidden but Assigned 2"
    };

    context.Transactions.AddRange(hiddenAssigned1, hiddenAssigned2);
    context.TransactionDetails.AddRange(detail1, detail2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ClearHiddenUnassigned.Handler(context);
    var command = new ClearHiddenUnassigned.Command();

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().Be(0);

    context.ChangeTracker.Clear();
    var transaction1 = await context.Transactions.FindAsync([822], TestContext.Current.CancellationToken);
    var transaction2 = await context.Transactions.FindAsync([823], TestContext.Current.CancellationToken);

    transaction1.Should().NotBeNull();
    transaction1!.TransactionHiddenFromAssign.Should().BeTrue();

    transaction2.Should().NotBeNull();
    transaction2!.TransactionHiddenFromAssign.Should().BeTrue();
  }

  [Fact]
  public async Task ClearHiddenUnassigned_WithNoUnassignedEnvelope_ReturnsZero()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var regularCategory = new Category {
      CategoryId = "1",
      Name = "Groceries",
      Description = "Groceries",
      SortOrder = 1,
      FamilyId = 1,
      CategoryType = CatTypes.User
    };
    var account = new BankAccount {
      Id = 830,
      Name = "Test Account",
      Balance = 1000m,
      AccountType = AccountTypes.Checking,
      FamilyId = 1
    };
    var regularEnvelope = new Envelope {
      Id = 831,
      Name = "Groceries",
      CategoryId = "1",
      Balance = 500m,
      FamilyId = 1,
      EnvelopeType = EnvelopeTypes.Standard,
      SortOrder = 1
    };
    var user = new User { Id = 1, Email = "TEST@TEST.COM", FirstName = "Test", LastName = "User", FamilyId = 1 };

    context.Families.Add(family);
    context.Categories.Add(regularCategory);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(regularEnvelope);
    context.Users.Add(user);

    // Create a hidden transaction (but no Unassigned envelope exists)
    var hiddenTransaction = new Transaction {
      Id = 832,
      AccountId = 830,
      Vendor = "Hidden Vendor",
      Date = DateTime.UtcNow,
      TotalAmount = 50m,
      UserId = 1,
      FamilyId = 1,
      TransactionHiddenFromAssign = true
    };
    var detail = new TransactionDetail {
      TransactionId = 832,
      LineId = 1,
      EnvelopeId = 831,
      Amount = 50m,
      Notes = "Hidden transaction"
    };

    context.Transactions.Add(hiddenTransaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new ClearHiddenUnassigned.Handler(context);
    var command = new ClearHiddenUnassigned.Command();

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().Be(0);

    context.ChangeTracker.Clear();
    var transaction = await context.Transactions.FindAsync([832], TestContext.Current.CancellationToken);

    transaction.Should().NotBeNull();
    transaction!.TransactionHiddenFromAssign.Should().BeTrue();
  }
}

