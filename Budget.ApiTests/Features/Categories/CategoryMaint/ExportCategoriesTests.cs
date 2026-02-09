using Budget.Api.Features.Categories.CategoryMaint;
using Microsoft.Extensions.Logging;
using Moq;

namespace Budget.ApiTests.Features.Categories.CategoryMaint;


/// <summary>
/// Unit tests for ExportCategories.Handler
/// </summary>
public class ExportCategoriesTests
{
    /// <summary>
    /// Creates DbContextOptions for an in-memory database with a unique name per test
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .Options;
    }

    /// <summary>
    /// Tests that Handle returns empty string when no categories exist
    /// </summary>
    [Fact]
    public async Task Handle_WithNoCategories_ReturnsEmptyString()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var family = new Family { Id = 1, Name = "Test Family" };
        context.Families.Add(family);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockLogger = new Mock<ILogger<ExportCategories.Handler>>();
        var handler = new ExportCategories.Handler(context, mockLogger.Object);
        var query = new ExportCategories.Query();

        // Act
        string result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();

        mockLogger.Verify(
          x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting category export to CSV")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);

        mockLogger.Verify(
          x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exported 0 categories to CSV")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);
    }

}


/// <summary>
/// Unit tests for ExportCategories.Endpoint
/// </summary>
public class ExportCategoriesEndpointTests
{
}