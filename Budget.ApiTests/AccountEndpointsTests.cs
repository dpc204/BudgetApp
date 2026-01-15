namespace Budget.ApiTests;

/// <summary>
/// Tests for Account API endpoints
/// </summary>
public class AccountEndpointsTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    => new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
      .Options;

  [Fact]
  public async Task GetAccounts_Should_Return_All_Accounts()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var account1 = new BankAccount 
    { 
      Id = 300, 
      Name = "Checking", 
      Balance = 1000m, 
      AccountType = BankAccount.AccountTypes.Checking,
      FamilyId = 1
    };
    var account2 = new BankAccount 
    { 
      Id = 301, 
      Name = "Credit Card", 
      Balance = 5000m, 
      AccountType = BankAccount.AccountTypes.Credit,
      FamilyId = 1
    };

    context.Families.Add(family);
    context.BankAccounts.AddRange(account1, account2);
    await context.SaveChangesAsync();

    var handler = new GetAll.Handler(context, NullLogger<GetAll.Handler>.Instance);

    // Act
    var result = await handler.Handle(new GetAll.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var resultList = result.ToList();
    resultList.Should().HaveCount(2);

    var acct1 = resultList.Should().ContainSingle(a => a.Id == 300).Subject;
    acct1.Name.Should().Be("Checking");
    acct1.Balance.Should().Be(1000m);
    acct1.AccountType.Should().Be(BankAccount.AccountTypes.Checking);

    var acct2 = resultList.Should().ContainSingle(a => a.Id == 301).Subject;
    acct2.Name.Should().Be("Credit Card");
    acct2.Balance.Should().Be(5000m);
    acct2.AccountType.Should().Be(BankAccount.AccountTypes.Credit);
  }

  [Fact]
  public async Task InsertAccount_Should_Create_New_Account()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    context.Families.Add(family);
    await context.SaveChangesAsync();

    var handler = new InsertAccount.Handler(context);
    var command = new InsertAccount.Command(
      Name: "New Account",
      Balance: 2500m,
      AccountType: BankAccount.AccountTypes.Checking);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be("New Account");
    result.Balance.Should().Be(2500m);
    result.AccountType.Should().Be(BankAccount.AccountTypes.Checking);
    result.Id.Should().BeGreaterThan(0);

    // Verify in database
    var savedAccount = await context.BankAccounts.FindAsync(result.Id);
    savedAccount.Should().NotBeNull();
    savedAccount!.Name.Should().Be("New Account");
    savedAccount.Balance.Should().Be(2500m);
  }

  [Fact]
  public async Task UpdateAccount_Should_Update_Existing_Account()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var account = new BankAccount 
    { 
      Id = 302, 
      Name = "Original Name", 
      Balance = 1000m, 
      AccountType = BankAccount.AccountTypes.Checking,
      FamilyId = 1
    };
    
    context.Families.Add(family);
    context.BankAccounts.Add(account);
    await context.SaveChangesAsync();

    var handler = new UpdateAccount.Handler(context);
    var command = new UpdateAccount.Command(
      Id: 302,
      Name: "Updated Name",
      Balance: 1500m,
      AccountType: BankAccount.AccountTypes.Credit);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(302);
    result.Name.Should().Be("Updated Name");
    result.Balance.Should().Be(1500m);
    result.AccountType.Should().Be(BankAccount.AccountTypes.Credit);

    // Verify in database
    context.ChangeTracker.Clear();
    var updatedAccount = await context.BankAccounts.FindAsync(302);
    updatedAccount.Should().NotBeNull();
    updatedAccount!.Name.Should().Be("Updated Name");
    updatedAccount.Balance.Should().Be(1500m);
    updatedAccount.AccountType.Should().Be(BankAccount.AccountTypes.Credit);
  }

  [Fact]
  public async Task UpdateAccount_With_NonExistent_Account_Should_Return_Null()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var handler = new UpdateAccount.Handler(context);
    var command = new UpdateAccount.Command(
      Id: 99999,
      Name: "Test",
      Balance: 100m,
      AccountType: BankAccount.AccountTypes.Checking);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task RemoveAccount_Should_Delete_Account()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var account = new BankAccount 
    { 
      Id = 304, 
      Name = "To Delete", 
      Balance = 500m, 
      AccountType = BankAccount.AccountTypes.Checking,
      FamilyId = 1
    };
    
    context.Families.Add(family);
    context.BankAccounts.Add(account);
    await context.SaveChangesAsync();

    var handler = new RemoveAccount.Handler(context);

    // Act
    var result = await handler.Handle(new RemoveAccount.Command(304), CancellationToken.None);

    // Assert
    result.Should().BeTrue();

    // Verify deletion in database
    context.ChangeTracker.Clear();
    var deletedAccount = await context.BankAccounts.FindAsync(304);
    deletedAccount.Should().BeNull();
  }

  [Fact]
  public async Task RemoveAccount_With_NonExistent_Account_Should_Return_False()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var handler = new RemoveAccount.Handler(context);

    // Act
    var result = await handler.Handle(new RemoveAccount.Command(99999), CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public async Task GetAccounts_With_Empty_Database_Should_Return_Empty_List()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetAll.Handler(context, NullLogger<GetAll.Handler>.Instance);

    // Act
    var result = await handler.Handle(new GetAll.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  [Fact]
  public async Task InsertAccount_Should_Set_FamilyId_From_CurrentUser()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    context.Families.Add(family);
    await context.SaveChangesAsync();

    var handler = new InsertAccount.Handler(context);
    var command = new InsertAccount.Command(
      Name: "Family Account",
      Balance: 1000m,
      AccountType: BankAccount.AccountTypes.Checking);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    var savedAccount = await context.BankAccounts.FindAsync(result.Id);
    savedAccount.Should().NotBeNull();
    savedAccount!.FamilyId.Should().Be(1); // Default family from CurrentUser context
  }

  [Fact]
  public async Task UpdateAccount_Should_Preserve_FamilyId()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    
    var family = new Family { Id = 1, Name = "Test Family" };
    var account = new BankAccount 
    { 
      Id = 305, 
      Name = "Original", 
      Balance = 1000m, 
      AccountType = BankAccount.AccountTypes.Checking,
      FamilyId = 1
    };
    
    context.Families.Add(family);
    context.BankAccounts.Add(account);
    await context.SaveChangesAsync();

    var handler = new UpdateAccount.Handler(context);
    var command = new UpdateAccount.Command(
      Id: 305,
      Name: "Updated",
      Balance: 2000m,
      AccountType: BankAccount.AccountTypes.Credit);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    context.ChangeTracker.Clear();
    var updatedAccount = await context.BankAccounts.FindAsync(305);
    updatedAccount!.FamilyId.Should().Be(1); // FamilyId should remain unchanged
  }
}
