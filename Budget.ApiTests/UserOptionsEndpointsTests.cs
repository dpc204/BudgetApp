using Budget.Api.Features.UserOptions;
using Budget.Shared.Enums;

namespace Budget.ApiTests;

/// <summary>
/// Tests for UserOptions API endpoints
/// </summary>
public class UserOptionsEndpointsTests : IntegrationTestBase
{
  /// <summary>
  /// Test SaveUserOptions endpoint - should save options to database
  /// </summary>
  [Fact]
  public async Task SaveUserOptions_Should_Save_Options_To_Database()
  {
    // Arrange
    var userId = "test-user-123";
    var options = new Budget.Shared.Services.UserOptions
    {
      FillAmountType = FillAmounts.FiftyPercent
    };
    var command = new SaveUserOptions.Command(userId, options);

    // Act
    var response = await Client.PostAsJsonAsync("/api/useroptions", command);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<SaveUserOptions.Response>();
    result.Should().NotBeNull();
    result!.Success.Should().BeTrue();

    // Verify in database
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
    var savedOptions = await db.SavedUserOptions.FindAsync(userId);
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
    var userId = "test-user-456";
    
    // First, save initial options
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      db.SavedUserOptions.Add(new SavedUserOptions
      {
        UserId = userId,
        JsonOptions = "{\"FillAmountType\":1}"
      });
      await db.SaveChangesAsync();
    }

    var updatedOptions = new Budget.Shared.Services.UserOptions
    {
      FillAmountType = FillAmounts.FillToBudget
    };
    var command = new SaveUserOptions.Command(userId, updatedOptions);

    // Act
    var response = await Client.PostAsJsonAsync("/api/useroptions", command);

    // Assert
    response.EnsureSuccessStatusCode();

    // Verify in database
    using var scope2 = _factory.Services.CreateScope();
    var db2 = scope2.ServiceProvider.GetRequiredService<BudgetContext>();
    var savedOptions = await db2.SavedUserOptions.FindAsync(userId);
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
    var userId = "test-user-789";
    
    // First, save some options
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      db.SavedUserOptions.Add(new SavedUserOptions
      {
        UserId = userId,
        JsonOptions = "{\"FillAmountType\":2}"
      });
      await db.SaveChangesAsync();
    }

    // Act
    var response = await Client.GetAsync($"/api/useroptions/{userId}");

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<GetUserOptions.Response>();
    result.Should().NotBeNull();
    result!.Options.Should().NotBeNull();
    result.Options!.FillAmountType.Should().Be(FillAmounts.FiftyPercent);
  }

  /// <summary>
  /// Test GetUserOptions endpoint - should return null for non-existent user
  /// </summary>
  [Fact]
  public async Task GetUserOptions_Should_Return_Null_For_NonExistent_User()
  {
    // Arrange
    var userId = "non-existent-user";

    // Act
    var response = await Client.GetAsync($"/api/useroptions/{userId}");

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<GetUserOptions.Response>();
    result.Should().NotBeNull();
    result!.Options.Should().BeNull();
  }
}
