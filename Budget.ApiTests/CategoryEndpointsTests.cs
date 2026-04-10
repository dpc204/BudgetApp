using Budget.Api.Features.Categories.CategoryMaint;
using Budget.Shared.Enums;
using CategoryGetByEnvelopeId = Budget.Api.Features.Categories.GetByEnvelopeId;

namespace Budget.ApiTests;

/// <summary>
/// Tests for Category API endpoints
/// </summary>
public class CategoryEndpointsTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    => new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
      .Options;

  [Fact]
  public async Task GetCategories_Should_Return_All_Categories()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category1 = new Category {
      CategoryId = "500",
      Name = "Food",
      Description = "Food expenses",
      SortOrder = 1,
      FamilyId = 1,
      CategoryType = CatTypes.Income
    };
    var category2 = new Category {
      CategoryId = "501",
      Name = "Transportation",
      Description = "Transportation expenses",
      SortOrder = 2,
      FamilyId = 1,
      CategoryType = CatTypes.User
    };

    context.Families.Add(family);
    context.Categories.AddRange(category1, category2);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new GetAllCategories.Handler(context);

    // Act
    IEnumerable<GetAllCategories.Response> result = await handler.Handle(new GetAllCategories.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var resultList = result.ToList();
    resultList.Should().HaveCount(2);

    GetAllCategories.Response cat1 = resultList.Should().ContainSingle(c => c.CategoryId == "500").Subject;
    cat1.Name.Should().Be("Food");
    cat1.Description.Should().Be("Food expenses");
    cat1.SortOrder.Should().Be(1);
  }

  [Fact]
  public async Task GetAllByEnvelopeId_Should_Return_All_Categories()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category {
      CategoryId = "502",
      Name = "Test Category",
      Description = "Test",
      SortOrder = 1,
      FamilyId = 1,
      CategoryType = CatTypes.User
    };

    context.Families.Add(family);
    context.Categories.Add(category);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new CategoryGetByEnvelopeId.Handler(context);

    // Act
    IEnumerable<CategoryGetByEnvelopeId.Response> result = await handler.Handle(new CategoryGetByEnvelopeId.Query(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var resultList = result.ToList();
    resultList.Should().HaveCount(1);
    resultList[0].CategoryId.Should().Be("502");
    resultList[0].Name.Should().Be("Test Category");
  }

  [Fact]
  public async Task InsertCategory_Should_Create_New_Category()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    context.Families.Add(family);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new InsertCategory.Handler(context);
    var command = new InsertCategory.Command(
      Name: "New Category",
      Description: "Test description",
      SortOrder: 10);

    // Act
    InsertCategory.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be("New Category");
    result.Description.Should().Be("Test description");
    result.SortOrder.Should().Be(10);
    result.CategoryId.Should().NotBeNullOrEmpty();

    // Verify in database
    Category? savedCategory = await context.Categories.FindAsync([result.CategoryId], TestContext.Current.CancellationToken);
    savedCategory.Should().NotBeNull();
    savedCategory!.Name.Should().Be("New Category");
    savedCategory.Description.Should().Be("Test description");
  }

  [Fact]
  public async Task UpdateCategory_Should_Update_Existing_Category()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category {
      CategoryId = "503",
      Name = "Original Name",
      Description = "Original",
      SortOrder = 1,
      FamilyId = 1,
      CategoryType = CatTypes.User
    };

    context.Families.Add(family);
    context.Categories.Add(category);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateCategory.Handler(context);
    var command = new UpdateCategory.Command(
      CategoryId: "503",
      Name: "Updated Name",
      Description: "Updated description",
      SortOrder: 5);

    // Act
    UpdateCategory.Response? result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.CategoryId.Should().Be("503");
    result.Name.Should().Be("Updated Name");
    result.Description.Should().Be("Updated description");
    result.SortOrder.Should().Be(5);

    // Verify in database
    context.ChangeTracker.Clear();
    Category? updatedCategory = await context.Categories.FindAsync(["503"], TestContext.Current.CancellationToken);
    updatedCategory.Should().NotBeNull();
    updatedCategory!.Name.Should().Be("Updated Name");
    updatedCategory.Description.Should().Be("Updated description");
  }

  [Fact]
  public async Task UpdateCategory_With_NonExistent_Category_Should_Return_Null()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var handler = new UpdateCategory.Handler(context);
    var command = new UpdateCategory.Command(
      CategoryId: "999",
      Name: "Test",
      Description: "Test",
      SortOrder: 1);

    // Act
    UpdateCategory.Response? result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task RemoveCategory_Should_Delete_Category()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    var category = new Category {
      CategoryId = "505",
      Name = "To Delete",
      Description = "Delete me",
      SortOrder = 1,
      FamilyId = 1,
      CategoryType = CatTypes.User
    };

    context.Families.Add(family);
    context.Categories.Add(category);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new RemoveCategory.Handler(context);

    // Act
    var result = await handler.Handle(new RemoveCategory.Command("505"), CancellationToken.None);

    // Assert
    result.Should().BeTrue();

    // Verify deletion in database
    context.ChangeTracker.Clear();
    Category? deletedCategory = await context.Categories.FindAsync(["505"], TestContext.Current.CancellationToken);
    deletedCategory.Should().BeNull();
  }

  [Fact]
  public async Task RemoveCategory_With_NonExistent_Category_Should_Return_False()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var handler = new RemoveCategory.Handler(context);

    // Act
    var result = await handler.Handle(new RemoveCategory.Command("99999"), CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }
}

