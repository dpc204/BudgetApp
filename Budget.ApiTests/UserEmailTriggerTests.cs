namespace Budget.ApiTests;

/// <summary>
/// Tests for User.Email uppercase trigger functionality
/// 
/// NOTE: These tests use SQL Server because triggers cannot be tested with EF Core InMemory provider.
/// The trigger is a database-level feature that only works with actual SQL Server.
/// 
/// To run these tests:
/// - Locally: SQL Server LocalDB will be used automatically
/// - CI/CD: Uses SQL Server from environment variables (LocalBudgetConnection or BudgetConnection)
/// </summary>
public class UserEmailTriggerTests : IDisposable
{
  private readonly BudgetContext _context;
  private readonly string _testDbName;
  
  public UserEmailTriggerTests()
  {
    // Use a unique database name for each test run
    _testDbName = $"BudgetTest_{Guid.NewGuid():N}";
    
    // Get connection string from environment or use LocalDB as fallback
    var connectionString = GetConnectionString();
    
    var options = new DbContextOptionsBuilder<BudgetContext>()
      .UseSqlServer(connectionString)
      .Options;
      
    _context = new BudgetContext(options);
    
    // Create database and run migrations
    _context.Database.Migrate();
  }
  
  private string GetConnectionString()
  {
    // Check for CI/CD environment variables first
    var ciConnectionString = Environment.GetEnvironmentVariable("LocalBudgetConnection") 
                             ?? Environment.GetEnvironmentVariable("BudgetConnection");
    
    if (!string.IsNullOrEmpty(ciConnectionString))
    {
      // Replace database name in CI connection string
      var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(ciConnectionString)
      {
        InitialCatalog = _testDbName
      };
      return builder.ConnectionString;
    }
    
    // Fallback to LocalDB for local development
    return $"Server=(localdb)\\mssqllocaldb;Database={_testDbName};Trusted_Connection=True;TrustServerCertificate=True;";
  }
  
  [Fact]
  public async Task Insert_User_WithLowercaseEmail_ConvertsToUppercase()
  {
    // Arrange
    var user = new User
    {
      Email = "test@example.com",
      FirstName = "Test",
      LastName = "User",
      FamilyId = 1
    };
    
    // Act
    _context.Users.Add(user);
    await _context.SaveChangesAsync();
    
    // Detach to force a fresh read from database
    _context.Entry(user).State = EntityState.Detached;
    
    // Assert - Read back from database
    var savedUser = await _context.Users.FindAsync(user.Id);
    Assert.NotNull(savedUser);
    Assert.Equal("TEST@EXAMPLE.COM", savedUser.Email);
  }
  
  [Fact]
  public async Task Update_User_WithLowercaseEmail_ConvertsToUppercase()
  {
    // Arrange - Create user with uppercase email
    var user = new User
    {
      Email = "ORIGINAL@EXAMPLE.COM",
      FirstName = "Test",
      LastName = "User",
      FamilyId = 1
    };
    _context.Users.Add(user);
    await _context.SaveChangesAsync();
    
    // Act - Update to lowercase
    user.Email = "updated@example.com";
    await _context.SaveChangesAsync();
    
    // Detach to force a fresh read from database
    _context.Entry(user).State = EntityState.Detached;
    
    // Assert - Read back from database
    var updatedUser = await _context.Users.FindAsync(user.Id);
    Assert.NotNull(updatedUser);
    Assert.Equal("UPDATED@EXAMPLE.COM", updatedUser.Email);
  }
  
  [Fact]
  public async Task Insert_User_WithUppercaseEmail_RemainsUppercase()
  {
    // Arrange
    var user = new User
    {
      Email = "TEST@EXAMPLE.COM",
      FirstName = "Test",
      LastName = "User",
      FamilyId = 1
    };
    
    // Act
    _context.Users.Add(user);
    await _context.SaveChangesAsync();
    
    // Detach to force a fresh read from database
    _context.Entry(user).State = EntityState.Detached;
    
    // Assert
    var savedUser = await _context.Users.FindAsync(user.Id);
    Assert.NotNull(savedUser);
    Assert.Equal("TEST@EXAMPLE.COM", savedUser.Email);
  }
  
  [Fact]
  public async Task Insert_User_WithMixedCaseEmail_ConvertsToUppercase()
  {
    // Arrange
    var user = new User
    {
      Email = "TeSt@ExAmPlE.CoM",
      FirstName = "Test",
      LastName = "User",
      FamilyId = 1
    };
    
    // Act
    _context.Users.Add(user);
    await _context.SaveChangesAsync();
    
    // Detach to force a fresh read from database
    _context.Entry(user).State = EntityState.Detached;
    
    // Assert
    var savedUser = await _context.Users.FindAsync(user.Id);
    Assert.NotNull(savedUser);
    Assert.Equal("TEST@EXAMPLE.COM", savedUser.Email);
  }
  
  public void Dispose()
  {
    // Clean up test database
    _context.Database.EnsureDeleted();
    _context.Dispose();
  }
}
