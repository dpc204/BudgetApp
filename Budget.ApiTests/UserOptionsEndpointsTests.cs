using Budget.Api.Features.UserOptions;
using Budget.Shared.Enums;
using FluentResults;

namespace Budget.ApiTests;

/// <summary>
/// Tests for UserOptions API endpoints
/// </summary>
public class UserOptionsEndpointsTests 
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
  /// Test SaveUserOptions endpoint - should save options to database
  /// </summary>
  [Fact]
  public async Task SaveUserOptions_Should_Save_Options_To_Database()
  {
    // Arrange
    var db = GetTestDBContext();
    var userId = 1;
    var options = new Budget.Shared.Services.UserOptions
    {
      FillAmountType = FillAmounts.FiftyPercent
    };
    var command = new SaveUserOptions.Command(userId, options);
    var handler = new SaveUserOptions.Handler(db);

    // Act
    var response = await handler.Handle(command, CancellationToken.None);

    // Assert
    response.Success.Should().Be(true);
    

    // Verify in database
    var savedOptions = await db.SavedUserOptions.FindAsync(new object[] { userId }, TestContext.Current.CancellationToken);
    savedOptions.Should().NotBeNull();
    savedOptions!.UserId.Should().Be(userId);
    savedOptions.JsonOptions.Should().Contain("\"FillAmountType\":2");
  }

  /// <summary>
  /// Test SaveUserOptions endpoint - should update existing options
  /// </summary>
  [Fact]
  public async Task SaveUserOptions_Should_Update_Existing_Options()
  {
    // Arrange
    var userId = 1;
    
    // First, save initial options
    var db = GetTestDBContext();
      db.SavedUserOptions.Add(new SavedUserOptions
      {
        UserId = userId,
        JsonOptions = "{\"FillAmountType\":1}"
      });
      await db.SaveChangesAsync(TestContext.Current.CancellationToken);

    var updatedOptions = new Budget.Shared.Services.UserOptions
    {
      FillAmountType = FillAmounts.FillToBudget
    };
    var command = new SaveUserOptions.Command(userId, updatedOptions);
    var handler = new SaveUserOptions.Handler(db);

    // Act
    var response = handler.Handle(command, CancellationToken.None);

    // Assert
    response.IsCompletedSuccessfully.Should().Be(true);

    // Verify in database
    var savedOptions = await db.SavedUserOptions.FindAsync(new object[] { userId }, TestContext.Current.CancellationToken);
    savedOptions.Should().NotBeNull();
    savedOptions!.JsonOptions.Should().Contain("\"FillAmountType\":3");
  }

  /// <summary>
  /// Test GetUserOptions endpoint - should return saved options
  /// </summary>
  [Fact]
  public async Task GetUserOptions_Should_Return_Saved_Options()
  {
    // Arrange
    var db = GetTestDBContext();
    var userId = 1;
    
    // First, save some options

      db.SavedUserOptions.Add(new SavedUserOptions
      {
        UserId = userId,
        JsonOptions = "{\"FillAmountType\":2}"
      });
      await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    

    // Act
    var command = new SaveUserOptions.Command(userId, new Budget.Shared.Services.UserOptions()
    {
      UserId = userId, FillAmountType = FillAmounts.OneHundredPercent, 
      SelectedCategoryType = "ALL"
    });
    var handler = new SaveUserOptions.Handler(db);

    var response = handler.Handle(command, CancellationToken.None);

    // Assert
    response.Result.Success.Should().Be(true);
    var rslt = await db.SavedUserOptions.FindAsync(new object[] { userId }, TestContext.Current.CancellationToken);
    rslt.Should().NotBeNull();
    rslt!.JsonOptions.Should().Contain("\"FillAmountType\":1");
  }

  /// <summary>
  /// Test GetUserOptions endpoint - should return null for non-existent user
  /// </summary>
  [Fact]
  public async Task GetUserOptions_Should_Return_Null_For_NonExistent_User()
  {
    // Arrange
    var db = GetTestDBContext();
    var userId = 1;

    var command = new GetUserOptions.Query(userId);
    var handler = new GetUserOptions.Handler(db, new NullLogger<GetUserOptions.Handler>());
    // Act
    var response = handler.Handle(command, CancellationToken.None);

    // Assert
    (await response).Options.Should().BeNull();
  }

  private class TestCurrentFamilyService : ICurrentFamilyService
  {
    public int FamilyId { get; set; } = 1;
    public int GetCurrentFamilyId() => FamilyId;
  }
}

