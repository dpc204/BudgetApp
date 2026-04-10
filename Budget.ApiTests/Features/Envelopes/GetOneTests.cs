using Budget.Api.Features.Envelopes;

namespace Budget.ApiTests.Features.Envelopes;


/// <summary>
/// Unit tests for GetOne.Handler
/// </summary>
public partial class GetOneTests
{
  /// <summary>
  /// Creates in-memory database options for testing
  /// </summary>
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
      .Options;
  }

  /// <summary>
  /// Tests that Handle returns Response with null envelope when the envelope ID does not exist.
  /// Input: EnvelopeId = 999 (non-existing)
  /// Expected: Returns Response with null Envelope
  /// </summary>
  [Fact]
  public async Task Handle_WithNonExistingEnvelopeId_ReturnsNullEnvelope()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetOne.Handler(context);

    // Act
    GetOne.Response result = await handler.Handle(new GetOne.Query(999), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Envelope.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle returns Response with null envelope when envelope ID is zero.
  /// Input: EnvelopeId = 0 (boundary value)
  /// Expected: Returns Response with null Envelope
  /// </summary>
  [Fact]
  public async Task Handle_WithZeroEnvelopeId_ReturnsNullEnvelope()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetOne.Handler(context);

    // Act
    GetOne.Response result = await handler.Handle(new GetOne.Query(0), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Envelope.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle returns Response with null envelope when envelope ID is negative.
  /// Input: EnvelopeId = -1 (negative value)
  /// Expected: Returns Response with null Envelope
  /// </summary>
  [Fact]
  public async Task Handle_WithNegativeEnvelopeId_ReturnsNullEnvelope()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetOne.Handler(context);

    // Act
    GetOne.Response result = await handler.Handle(new GetOne.Query(-1), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Envelope.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle returns Response with null envelope when envelope ID is int.MaxValue.
  /// Input: EnvelopeId = int.MaxValue (boundary value)
  /// Expected: Returns Response with null Envelope
  /// </summary>
  [Fact]
  public async Task Handle_WithMaxIntEnvelopeId_ReturnsNullEnvelope()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetOne.Handler(context);

    // Act
    GetOne.Response result = await handler.Handle(new GetOne.Query(int.MaxValue), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Envelope.Should().BeNull();
  }

  /// <summary>
  /// Tests that Handle returns Response with null envelope when envelope ID is int.MinValue.
  /// Input: EnvelopeId = int.MinValue (boundary value)
  /// Expected: Returns Response with null Envelope
  /// </summary>
  [Fact]
  public async Task Handle_WithMinIntEnvelopeId_ReturnsNullEnvelope()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new GetOne.Handler(context);

    // Act
    GetOne.Response result = await handler.Handle(new GetOne.Query(int.MinValue), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Envelope.Should().BeNull();
  }

}
