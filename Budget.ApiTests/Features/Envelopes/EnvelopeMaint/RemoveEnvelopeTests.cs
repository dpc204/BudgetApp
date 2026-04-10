using Budget.Api.Features.Envelopes.EnvelopeMaint;

namespace Budget.ApiTests.Features.Envelopes.EnvelopeMaint;


/// <summary>
/// Unit tests for RemoveEnvelope.Handler
/// </summary>
public class RemoveEnvelopeTests
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
  /// Tests that Handle returns false when envelope does not exist.
  /// Input: Command with Id that does not exist in database
  /// Expected: Returns false, no changes to database
  /// </summary>
  [Fact]
  public async Task Handle_WithNonExistingId_ReturnsFalse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new RemoveEnvelope.Handler(context);
    var command = new RemoveEnvelope.Command(999);

    // Act
    bool result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  /// <summary>
  /// Tests that Handle returns false for zero Id when no envelope exists.
  /// Input: Command with Id = 0 (boundary value) when no envelope with Id 0 exists
  /// Expected: Returns false
  /// </summary>
  [Fact]
  public async Task Handle_WithIdZero_WhenNotExists_ReturnsFalse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new RemoveEnvelope.Handler(context);
    var command = new RemoveEnvelope.Command(0);

    // Act
    bool result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  /// <summary>
  /// Tests that Handle returns false for negative Id values.
  /// Input: Command with negative Id values
  /// Expected: Returns false
  /// </summary>
  [Theory]
  [InlineData(-1)]
  [InlineData(-100)]
  [InlineData(int.MinValue)]
  public async Task Handle_WithNegativeId_ReturnsFalse(int negativeId)
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new RemoveEnvelope.Handler(context);
    var command = new RemoveEnvelope.Command(negativeId);

    // Act
    bool result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  /// <summary>
  /// Tests that Handle returns false for maximum integer Id when not found.
  /// Input: Command with Id = int.MaxValue (boundary value)
  /// Expected: Returns false
  /// </summary>
  [Fact]
  public async Task Handle_WithMaxIntId_WhenNotExists_ReturnsFalse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new RemoveEnvelope.Handler(context);
    var command = new RemoveEnvelope.Command(int.MaxValue);

    // Act
    bool result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

}
