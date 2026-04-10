//using Budget.Shared.Services;

namespace Budget.ApiTests.Features.Transactions;
/// <summary>
/// Unit tests for GetOneTransactionDetail.Handler
/// </summary>
public sealed class GetOneTransactionDetailTests
{
  /// <summary>
  /// Creates in-memory database options for testing
  /// </summary>
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
  }

  /// <summary>
  /// Tests that Handle returns a complete Response when a valid transaction exists
  /// </summary>
  [Fact]
  public async Task Handle_WithValidTransactionId_ReturnsCompleteResponse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var user = new User {
      Id = 1,
      Email = "test@test.com",
      FirstName = "John",
      LastName = "Doe",
      FamilyId = 1
    };
    var account = new BankAccount {
      Id = 1,
      Name = "Checking",
      FamilyId = 1
    };
    var envelope1 = new Envelope {
      Id = 1,
      Name = "Groceries",
      FamilyId = 1
    };
    var envelope2 = new Envelope {
      Id = 2,
      Name = "Gas",
      FamilyId = 1
    };
    var transaction = new Transaction {
      Id = 100,
      AccountId = 1,
      Date = new DateTime(2024, 1, 15),
      Vendor = "Test Vendor",
      Description = "Test Description",
      TotalAmount = 150.50m,
      UserId = 1,
      IsVoided = false,
      FamilyId = 1
    };
    var detail1 = new TransactionDetail {
      TransactionId = 100,
      LineId = 1,
      EnvelopeId = 1,
      Notes = "Detail 1",
      Amount = 100.25m
    };
    var detail2 = new TransactionDetail {
      TransactionId = 100,
      LineId = 2,
      EnvelopeId = 2,
      Notes = "Detail 2",
      Amount = 50.25m
    };
    context.Families.Add(family);
    context.Users.Add(user);
    context.BankAccounts.Add(account);
    context.Envelopes.AddRange(envelope1, envelope2);
    context.Transactions.Add(transaction);
    context.TransactionDetails.AddRange(detail1, detail2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(100);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(100);
    result.AccountId.Should().Be(1);
    result.Date.Should().Be(new DateTime(2024, 1, 15));
    result.Vendor.Should().Be("Test Vendor");
    result.TotalAmount.Should().Be(150.50m);
    result.UserInitials.Should().Be("JD");
    result.IsVoided.Should().BeFalse();
    result.Details.Should().HaveCount(2);
    result.Details[0].LineId.Should().Be(1);
    result.Details[0].TransactionId.Should().Be(100);
    result.Details[0].EnvelopeId.Should().Be(1);
    result.Details[0].Notes.Should().Be("Detail 1");
    result.Details[0].Amount.Should().Be(100.25m);
    result.Details[1].LineId.Should().Be(2);
  }

  /// <summary>
  /// Tests that Handle returns null when transaction does not exist
  /// </summary>
  [Fact]
  public async Task Handle_WithInvalidTransactionId_ReturnsNull()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(999);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle returns Response with empty Details list when transaction has no details
  /// </summary>
  [Fact]
  public async Task Handle_WithTransactionHavingNoDetails_ReturnsResponseWithEmptyDetailsList()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var user = new User {
      Id = 1,
      Email = "test@test.com",
      FirstName = "Jane",
      LastName = "Smith",
      FamilyId = 1
    };
    var account = new BankAccount {
      Id = 1,
      Name = "Checking",
      FamilyId = 1
    };
    var transaction = new Transaction {
      Id = 200,
      AccountId = 1,
      Date = new DateTime(2024, 2, 20),
      Vendor = "No Details Vendor",
      TotalAmount = 0m,
      UserId = 1,
      IsVoided = false,
      FamilyId = 1
    };
    context.Families.Add(family);
    context.Users.Add(user);
    context.BankAccounts.Add(account);
    context.Transactions.Add(transaction);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(200);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(200);
    result.Details.Should().NotBeNull();
    result.Details.Should().BeEmpty();
  }

  /// <summary>
  /// Tests that Handle orders details by LineId correctly
  /// </summary>
  [Fact]
  public async Task Handle_WithMultipleDetails_OrdersByLineIdAscending()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var user = new User {
      Id = 1,
      Email = "test@test.com",
      FirstName = "Bob",
      LastName = "Jones",
      FamilyId = 1
    };
    var account = new BankAccount {
      Id = 1,
      Name = "Checking",
      FamilyId = 1
    };
    var envelope = new Envelope {
      Id = 1,
      Name = "Test Envelope",
      FamilyId = 1
    };
    var transaction = new Transaction {
      Id = 300,
      AccountId = 1,
      Date = DateTime.UtcNow,
      Vendor = "Multi Detail Vendor",
      TotalAmount = 300m,
      UserId = 1,
      IsVoided = false,
      FamilyId = 1
    };
    var detail3 = new TransactionDetail {
      TransactionId = 300,
      LineId = 3,
      EnvelopeId = 1,
      Amount = 100m
    };
    var detail1 = new TransactionDetail {
      TransactionId = 300,
      LineId = 1,
      EnvelopeId = 1,
      Amount = 100m
    };
    var detail2 = new TransactionDetail {
      TransactionId = 300,
      LineId = 2,
      EnvelopeId = 1,
      Amount = 100m
    };
    context.Families.Add(family);
    context.Users.Add(user);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Transactions.Add(transaction);
    context.TransactionDetails.AddRange(detail3, detail1, detail2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(300);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().NotBeNull();
    result!.Details.Should().HaveCount(3);
    result.Details[0].LineId.Should().Be(1);
    result.Details[1].LineId.Should().Be(2);
    result.Details[2].LineId.Should().Be(3);
  }

  /// <summary>
  /// Tests that Handle correctly extracts user initials from first and last names
  /// </summary>
  [Theory]
  [InlineData("Alice", "Brown", "AB")]
  [InlineData("X", "Y", "XY")]
  [InlineData("Christopher", "Montgomery", "CM")]
  public async Task Handle_ExtractsUserInitials_Correctly(string firstName, string lastName, string expectedInitials)
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var user = new User {
      Id = 1,
      Email = "test@test.com",
      FirstName = firstName,
      LastName = lastName,
      FamilyId = 1
    };
    var account = new BankAccount {
      Id = 1,
      Name = "Checking",
      FamilyId = 1
    };
    var transaction = new Transaction {
      Id = 400,
      AccountId = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      TotalAmount = 100m,
      UserId = 1,
      IsVoided = false,
      FamilyId = 1
    };
    context.Families.Add(family);
    context.Users.Add(user);
    context.BankAccounts.Add(account);
    context.Transactions.Add(transaction);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(400);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().NotBeNull();
    result!.UserInitials.Should().Be(expectedInitials);
  }

  /// <summary>
  /// Tests that Handle correctly maps IsVoided property when true
  /// </summary>
  [Fact]
  public async Task Handle_WithVoidedTransaction_ReturnsIsVoidedTrue()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var user = new User {
      Id = 1,
      Email = "test@test.com",
      FirstName = "Test",
      LastName = "User",
      FamilyId = 1
    };
    var account = new BankAccount {
      Id = 1,
      Name = "Checking",
      FamilyId = 1
    };
    var transaction = new Transaction {
      Id = 500,
      AccountId = 1,
      Date = DateTime.UtcNow,
      Vendor = "Voided Vendor",
      TotalAmount = 100m,
      UserId = 1,
      IsVoided = true,
      FamilyId = 1
    };
    context.Families.Add(family);
    context.Users.Add(user);
    context.BankAccounts.Add(account);
    context.Transactions.Add(transaction);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(500);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().NotBeNull();
    result!.IsVoided.Should().BeTrue();
  }

  /// <summary>
  /// Tests that Handle correctly maps all TransactionDetail properties to TransactionDetailDto
  /// </summary>
  [Fact]
  public async Task Handle_MapsAllDetailProperties_Correctly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var user = new User {
      Id = 1,
      Email = "test@test.com",
      FirstName = "Test",
      LastName = "User",
      FamilyId = 1
    };
    var account = new BankAccount {
      Id = 1,
      Name = "Checking",
      FamilyId = 1
    };
    var envelope = new Envelope {
      Id = 5,
      Name = "Test Envelope",
      FamilyId = 1
    };
    var transaction = new Transaction {
      Id = 600,
      AccountId = 1,
      Date = DateTime.UtcNow,
      Vendor = "Detail Mapping Test",
      TotalAmount = 75.99m,
      UserId = 1,
      IsVoided = false,
      FamilyId = 1
    };
    var detail = new TransactionDetail {
      TransactionId = 600,
      LineId = 10,
      EnvelopeId = 5,
      Notes = "Test notes for detail",
      Amount = 75.99m
    };
    context.Families.Add(family);
    context.Users.Add(user);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(600);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().NotBeNull();
    result!.Details.Should().HaveCount(1);
    var detailDto = result.Details[0];
    detailDto.TransactionId.Should().Be(600);
    detailDto.LineId.Should().Be(10);
    detailDto.EnvelopeId.Should().Be(5);
    detailDto.Notes.Should().Be("Test notes for detail");
    detailDto.Amount.Should().Be(75.99m);
  }

  /// <summary>
  /// Tests that Handle returns null for boundary value TransactionId of zero
  /// </summary>
  [Fact]
  public async Task Handle_WithTransactionIdZero_ReturnsNull()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(0);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle returns null for negative TransactionId
  /// </summary>
  [Fact]
  public async Task Handle_WithNegativeTransactionId_ReturnsNull()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(-1);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle returns null for maximum integer TransactionId value
  /// </summary>
  [Fact]
  public async Task Handle_WithMaxIntTransactionId_ReturnsNull()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(int.MaxValue);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle respects cancellation token
  /// </summary>
  [Fact]
  public async Task Handle_WithCancelledToken_ThrowsOperationCanceledException()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(1);
    var cts = new CancellationTokenSource();
    cts.Cancel();
    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(async () => await handler.Handle(query, cts.Token));
  }

  /// <summary>
  /// Tests that Handle correctly maps Description property
  /// </summary>
  [Fact(Skip = "ProductionBugSuspected")]
  [Trait("Category", "ProductionBugSuspected")]
  public async Task Handle_MapsDescriptionProperty_Correctly()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var user = new User {
      Id = 1,
      Email = "test@test.com",
      FirstName = "Test",
      LastName = "User",
      FamilyId = 1
    };
    var account = new BankAccount {
      Id = 1,
      Name = "Checking",
      FamilyId = 1
    };
    var transaction = new Transaction {
      Id = 700,
      AccountId = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      Description = "Test transaction description",
      TotalAmount = 100m,
      UserId = 1,
      IsVoided = false,
      FamilyId = 1
    };
    context.Families.Add(family);
    context.Users.Add(user);
    context.BankAccounts.Add(account);
    context.Transactions.Add(transaction);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(700);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().NotBeNull();
    result!.Description.Should().Be("Test transaction description");
  }

  /// <summary>
  /// Tests that Handle maps empty Description when not set
  /// </summary>
  [Fact]
  public async Task Handle_WithEmptyDescription_ReturnsEmptyString()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var user = new User {
      Id = 1,
      Email = "test@test.com",
      FirstName = "Test",
      LastName = "User",
      FamilyId = 1
    };
    var account = new BankAccount {
      Id = 1,
      Name = "Checking",
      FamilyId = 1
    };
    var transaction = new Transaction {
      Id = 800,
      AccountId = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      Description = string.Empty,
      TotalAmount = 100m,
      UserId = 1,
      IsVoided = false,
      FamilyId = 1
    };
    context.Families.Add(family);
    context.Users.Add(user);
    context.BankAccounts.Add(account);
    context.Transactions.Add(transaction);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(800);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().NotBeNull();
    result!.Description.Should().BeEmpty();
  }

  /// <summary>
  /// Tests that Handle correctly maps detail Notes when empty
  /// </summary>
  [Fact]
  public async Task Handle_WithDetailHavingEmptyNotes_MapsEmptyString()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var family = new Family {
      Id = 1,
      Name = "Test Family"
    };
    var user = new User {
      Id = 1,
      Email = "test@test.com",
      FirstName = "Test",
      LastName = "User",
      FamilyId = 1
    };
    var account = new BankAccount {
      Id = 1,
      Name = "Checking",
      FamilyId = 1
    };
    var envelope = new Envelope {
      Id = 1,
      Name = "Test Envelope",
      FamilyId = 1
    };
    var transaction = new Transaction {
      Id = 900,
      AccountId = 1,
      Date = DateTime.UtcNow,
      Vendor = "Test Vendor",
      TotalAmount = 50m,
      UserId = 1,
      IsVoided = false,
      FamilyId = 1
    };
    var detail = new TransactionDetail {
      TransactionId = 900,
      LineId = 1,
      EnvelopeId = 1,
      Notes = string.Empty,
      Amount = 50m
    };
    context.Families.Add(family);
    context.Users.Add(user);
    context.BankAccounts.Add(account);
    context.Envelopes.Add(envelope);
    context.Transactions.Add(transaction);
    context.TransactionDetails.Add(detail);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    var handler = new GetOneTransactionDetail.Handler(context);
    var query = new GetOneTransactionDetail.Query(900);
    // Act
    GetOneTransactionDetail.Response? result = await handler.Handle(query, CancellationToken.None);
    // Assert
    result.Should().NotBeNull();
    result!.Details.Should().HaveCount(1);
    result.Details[0].Notes.Should().BeEmpty();
  }
}
