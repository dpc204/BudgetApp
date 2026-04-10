using Budget.Api.Features.Envelopes;
using Budget.Shared.Enums;

namespace Budget.ApiTests.Features.Envelopes;


/// <summary>
/// Tests for the GetByEnvelopeType.Handler class.
/// </summary>
public partial class GetByEnvelopeTypeHandlerTests
{
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;
  }

  /// <summary>
  /// Tests that Handle returns null when the database is empty.
  /// Input: Any EnvelopeType with an empty database.
  /// Expected: Returns null.
  /// </summary>
  [Fact]
  public async Task Handle_WithEmptyDatabase_ReturnsNull()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetByEnvelopeType.Handler(context);
    var query = new GetByEnvelopeType.Query(EnvelopeTypes.Unassigned);

    // Act
    EnvelopeDto? result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

}
