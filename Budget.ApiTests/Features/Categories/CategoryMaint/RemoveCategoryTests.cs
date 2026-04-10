using Budget.Api.Features.Categories.CategoryMaint;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Budget.ApiTests.Features.Categories.CategoryMaint;


/// <summary>
/// Unit tests for RemoveCategory.Handler
/// </summary>
public class HandlerTests
{
  /// <summary>
  /// Creates in-memory database options for testing
  /// </summary>
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;
  }

  /// <summary>
  /// Tests that Handle returns false when the CategoryId does not exist in the database
  /// </summary>
  [Fact]
  public async Task Handle_NonExistentCategoryId_ReturnsFalse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    context.Families.Add(family);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new RemoveCategory.Handler(context);
    var command = new RemoveCategory.Command("non-existent-id");

    // Act
    bool result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  /// <summary>
  /// Tests that Handle returns false when CategoryId is null
  /// </summary>
  [Fact]
  public async Task Handle_NullCategoryId_ReturnsFalse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    context.Families.Add(family);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new RemoveCategory.Handler(context);
    var command = new RemoveCategory.Command(null!);

    // Act
    bool result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  /// <summary>
  /// Tests that Handle returns false when CategoryId is an empty string
  /// </summary>
  [Fact]
  public async Task Handle_EmptyCategoryId_ReturnsFalse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    context.Families.Add(family);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new RemoveCategory.Handler(context);
    var command = new RemoveCategory.Command(string.Empty);

    // Act
    bool result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  /// <summary>
  /// Tests that Handle returns false when CategoryId is whitespace only
  /// </summary>
  [Fact]
  public async Task Handle_WhitespaceCategoryId_ReturnsFalse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var family = new Family { Id = 1, Name = "Test Family" };
    context.Families.Add(family);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new RemoveCategory.Handler(context);
    var command = new RemoveCategory.Command("   ");

    // Act
    bool result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

}


/// <summary>
/// Unit tests for the RemoveCategory.Endpoint class.
/// </summary>
public partial class EndpointTests
{
  /// <summary>
  /// Tests that AddRoutes completes without throwing when provided with a valid IEndpointRouteBuilder.
  /// Input: Valid mocked IEndpointRouteBuilder
  /// Expected: Method completes successfully without throwing
  /// </summary>
  [Fact]
  public void AddRoutes_WithValidRouteBuilder_CompletesWithoutThrowing()
  {
    // Arrange
    var mockEndpointRouteBuilder = new Mock<IEndpointRouteBuilder>();
    var mockServiceProvider = new Mock<IServiceProvider>();
    var mockApplicationBuilder = new Mock<IApplicationBuilder>();

    mockApplicationBuilder.Setup(x => x.ApplicationServices).Returns(mockServiceProvider.Object);
    mockEndpointRouteBuilder.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
    mockEndpointRouteBuilder.Setup(x => x.CreateApplicationBuilder()).Returns(mockApplicationBuilder.Object);

    var mockDataSources = new List<EndpointDataSource>();
    mockEndpointRouteBuilder.Setup(x => x.DataSources).Returns(mockDataSources);

    var endpoint = new RemoveCategory.Endpoint();

    // Act & Assert
    // Note: Full verification of MapDelete call and RequireAuthorization would require integration testing
    // or a testable abstraction around endpoint registration. This test verifies basic invocability.
    var exception = Record.Exception(() => endpoint.AddRoutes(mockEndpointRouteBuilder.Object));

    Assert.Null(exception);
  }

  /// <summary>
  /// Tests that AddRoutes throws ArgumentNullException when provided with null IEndpointRouteBuilder.
  /// Input: null IEndpointRouteBuilder
  /// Expected: ArgumentNullException or NullReferenceException
  /// </summary>
  [Fact]
  public void AddRoutes_WithNullRouteBuilder_ThrowsException()
  {
    // Arrange
    var endpoint = new RemoveCategory.Endpoint();

    // Act & Assert
    // The method will throw when attempting to call MapDelete on null
    Assert.ThrowsAny<Exception>(() => endpoint.AddRoutes(null!));
  }
}