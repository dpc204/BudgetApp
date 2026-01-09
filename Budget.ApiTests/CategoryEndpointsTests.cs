using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Budget.Api.Features.Categories.CategoryMaint;
using Budget.DB;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CategoryGetAll = Budget.Api.Features.Categories.GetByEnvelopeId;

namespace Budget.ApiTests;

/// <summary>
/// Tests for Category API endpoints
/// </summary>
public class CategoryEndpointsTests : IntegrationTestBase
{

  /// <summary>
  /// Test GetCategories endpoint - should return all categories
  /// </summary>
  [Fact]
  public async Task GetCategories_Should_Return_All_Categories()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

    var category1 = TestHelpers.CreateTestCategory(id: "500", name: "Food", sortOrder: 1);
    var category2 = TestHelpers.CreateTestCategory(id: "501", name: "Transportation", sortOrder: 2);

    db.Categories.Add(category1);
    db.Categories.Add(category2);
    await db.SaveChangesAsync();

    // Act
    var response = await Client.GetAsync("/categories/maint/getall");

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<List<GetAll.Response>>();

    result.Should().NotBeNull();
    result.Should().HaveCount(c => c >= 2);

    var cat1 = result!.FirstOrDefault(c => c.CategoryId == "500");
    cat1.Should().NotBeNull();
    cat1!.Name.Should().Be("Food");
  }

  /// <summary>
  /// Test GetAll (getbyenvelopeid) endpoint - should return all categories
  /// </summary>
  [Fact]
  public async Task GetAllByEnvelopeId_Should_Return_All_Categories()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

    var category = TestHelpers.CreateTestCategory(id: "502", name: "Test Category", sortOrder: 1);
    db.Categories.Add(category);
    await db.SaveChangesAsync();

    // Act
    var response = await Client.GetAsync("/categories/getbyenvelopeid");

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<List<CategoryGetAll.Response>>();

    result.Should().NotBeNull();
    result.Should().HaveCount(c => c >= 1);
  }

  /// <summary>
  /// Test InsertCategory endpoint - should create a new category
  /// </summary>
  [Fact]
  public async Task InsertCategory_Should_Create_New_Category()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
    var command = new InsertCategory.Command(
      Name: "New Category",
      Description: "Test description",
      SortOrder: 10);

    // Act
    var response = await Client.PostAsJsonAsync("/categories/maint/Insert", command);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<InsertCategory.Response>();

    result.Should().NotBeNull();
    result!.Name.Should().Be("New Category");
    result.Description.Should().Be("Test description");
    result.SortOrder.Should().Be(10);
    result.CategoryId.Should().NotBeNullOrEmpty();

    // Verify in database
    db.ChangeTracker.Clear();
    var savedCategory = await db.Categories.FindAsync(result.CategoryId);

    savedCategory.Should().NotBeNull();
    savedCategory!.Name.Should().Be("New Category");
  }

  /// <summary>
  /// Test UpdateCategory endpoint - should update an existing category
  /// </summary>
  [Fact]
  public async Task UpdateCategory_Should_Update_Existing_Category()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

    var category = TestHelpers.CreateTestCategory(id: "503", name: "Original Name", sortOrder: 1);
    db.Categories.Add(category);
    await db.SaveChangesAsync();

    var commandBody = new UpdateCategory.CommandBody
    {
      CategoryId = "503",
      Name = "Updated Name",
      Description = "Updated description",
      SortOrder = 5
    };

    // Act
    var response = await Client.PutAsJsonAsync("/categories/maint/503", commandBody);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<UpdateCategory.Response>();

    result.Should().NotBeNull();
    result!.CategoryId.Should().Be("503");
    result.Name.Should().Be("Updated Name");
    result.Description.Should().Be("Updated description");
    result.SortOrder.Should().Be(5);

    // Verify in database
    db.ChangeTracker.Clear();
    var updatedCategory = await db.Categories.FindAsync("503");

    updatedCategory.Should().NotBeNull();
    updatedCategory!.Name.Should().Be("Updated Name");
  }

  /// <summary>
  /// Test UpdateCategory endpoint with mismatched IDs - should return BadRequest
  /// </summary>
  [Fact]
  public async Task UpdateCategory_With_Mismatched_Ids_Should_Return_BadRequest()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
    var commandBody = new UpdateCategory.CommandBody
    {
      CategoryId = "999",
      Name = "Test",
      Description = "Test",
      SortOrder = 1
    };

    // Act
    var response = await Client.PutAsJsonAsync("/categories/maint/504", commandBody);

    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
  }

  /// <summary>
  /// Test RemoveCategory endpoint - should delete a category
  /// </summary>
  [Fact]
  public async Task RemoveCategory_Should_Delete_Category()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

    var category = TestHelpers.CreateTestCategory(id: "505", name: "To Delete", sortOrder: 1);
    db.Categories.Add(category);
    await db.SaveChangesAsync();

    // Act
    var response = await Client.DeleteAsync("/categories/maint/505");

    // Assert
    response.EnsureSuccessStatusCode();
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

    // Verify deletion in database
    db.ChangeTracker.Clear();
    var deletedCategory = await db.Categories.FindAsync("505");
    deletedCategory.Should().BeNull();
  }

  /// <summary>
  /// Test RemoveCategory endpoint with non-existent category - should return NotFound
  /// </summary>
  [Fact]
  public async Task RemoveCategory_With_NonExistent_Category_Should_Return_NotFound()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

    // Act
    var response = await Client.DeleteAsync("/categories/maint/99999");

    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
  }
}
