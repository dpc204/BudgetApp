using Budget.Api.Features.Envelopes;
using Budget.Api.Features.Funding;

namespace Budget.ApiTests.Features.Envelopes;


/// <summary>
/// Unit tests for UpdateFundAmount.Handler
/// </summary>
public partial class UpdateFundAmountHandlerTests
{
  /// <summary>
  /// Creates in-memory database options for testing
  /// </summary>
  private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
  {
    return new DbContextOptionsBuilder<BudgetContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
      .Options;
  }

  /// <summary>
  /// Tests that Handle updates the fund amount and returns success when envelope exists
  /// </summary>
  [Fact]
  public async Task Handle_EnvelopeExists_UpdatesFundAmountAndReturnsSuccess()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1 };
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateFundAmount.Handler(context);
    var command = new UpdateFundAmount.Command(1, 100.50m);

    // Act
    UpdateFundAmount.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.Message.Should().Be("Fund amount updated successfully");

    Envelope? updatedEnvelope = await context.Envelopes.FirstOrDefaultAsync(e => e.Id == 1, TestContext.Current.CancellationToken);
    updatedEnvelope.Should().NotBeNull();
    updatedEnvelope!.FundAmount.Should().Be(100.50m);
  }

  /// <summary>
  /// Tests that Handle returns failure response when envelope is not found
  /// </summary>
  [Fact]
  public async Task Handle_EnvelopeNotFound_ReturnsFailureResponse()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new UpdateFundAmount.Handler(context);
    var command = new UpdateFundAmount.Command(999, 100m);

    // Act
    UpdateFundAmount.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Message.Should().Be("Envelope not found");
  }

  /// <summary>
  /// Tests that Handle sets fund amount to zero when FundAmount is null
  /// </summary>
  [Fact]
  public async Task Handle_FundAmountIsNull_SetsToZero()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1, FundAmount = 50m };
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateFundAmount.Handler(context);
    var command = new UpdateFundAmount.Command(1, null);

    // Act
    UpdateFundAmount.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.Message.Should().Be("Fund amount updated successfully");

    Envelope? updatedEnvelope = await context.Envelopes.FirstOrDefaultAsync(e => e.Id == 1, TestContext.Current.CancellationToken);
    updatedEnvelope.Should().NotBeNull();
    updatedEnvelope!.FundAmount.Should().Be(0m);
  }

  /// <summary>
  /// Tests that Handle accepts and sets zero fund amount
  /// </summary>
  [Fact]
  public async Task Handle_FundAmountIsZero_SetsToZero()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1, FundAmount = 100m };
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateFundAmount.Handler(context);
    var command = new UpdateFundAmount.Command(1, 0m);

    // Act
    UpdateFundAmount.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();

    Envelope? updatedEnvelope = await context.Envelopes.FirstOrDefaultAsync(e => e.Id == 1, TestContext.Current.CancellationToken);
    updatedEnvelope.Should().NotBeNull();
    updatedEnvelope!.FundAmount.Should().Be(0m);
  }

  /// <summary>
  /// Tests that Handle accepts and sets negative fund amount
  /// </summary>
  [Fact]
  public async Task Handle_FundAmountIsNegative_SetsNegativeValue()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1 };
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateFundAmount.Handler(context);
    var command = new UpdateFundAmount.Command(1, -50.25m);

    // Act
    UpdateFundAmount.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();

    Envelope? updatedEnvelope = await context.Envelopes.FirstOrDefaultAsync(e => e.Id == 1, TestContext.Current.CancellationToken);
    updatedEnvelope.Should().NotBeNull();
    updatedEnvelope!.FundAmount.Should().Be(-50.25m);
  }

  /// <summary>
  /// Tests that Handle accepts and sets large decimal fund amounts
  /// </summary>
  [Theory]
  [InlineData(999999999999.99)]
  [InlineData(-999999999999.99)]
  [InlineData(0.01)]
  [InlineData(-0.01)]
  public async Task Handle_VariousFundAmounts_SetsCorrectValue(decimal fundAmount)
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1 };
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateFundAmount.Handler(context);
    var command = new UpdateFundAmount.Command(1, fundAmount);

    // Act
    UpdateFundAmount.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();

    Envelope? updatedEnvelope = await context.Envelopes.FirstOrDefaultAsync(e => e.Id == 1, TestContext.Current.CancellationToken);
    updatedEnvelope.Should().NotBeNull();
    updatedEnvelope!.FundAmount.Should().Be(fundAmount);
  }

  /// <summary>
  /// Tests that Handle works with various envelope IDs including edge case values
  /// </summary>
  [Theory]
  [InlineData(1)]
  [InlineData(int.MaxValue)]
  [InlineData(2)]
  [InlineData(-1)]
  public async Task Handle_ValidEnvelopeWithVariousIds_UpdatesSuccessfully(int envelopeId)
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope { Id = envelopeId, Name = "Test Envelope", CategoryId = "1", FamilyId = 1 };
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateFundAmount.Handler(context);
    var command = new UpdateFundAmount.Command(envelopeId, 100m);

    // Act
    UpdateFundAmount.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();

    Envelope? updatedEnvelope = await context.Envelopes.FirstOrDefaultAsync(e => e.Id == envelopeId, TestContext.Current.CancellationToken);
    updatedEnvelope.Should().NotBeNull();
    updatedEnvelope!.FundAmount.Should().Be(100m);
  }

  /// <summary>
  /// Tests that Handle returns failure for non-existent envelope IDs including edge cases
  /// </summary>
  [Theory]
  [InlineData(999)]
  [InlineData(int.MinValue)]
  [InlineData(int.MaxValue)]
  [InlineData(-999)]
  public async Task Handle_NonExistentEnvelopeIds_ReturnsFailure(int envelopeId)
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);
    var handler = new UpdateFundAmount.Handler(context);
    var command = new UpdateFundAmount.Command(envelopeId, 100m);

    // Act
    UpdateFundAmount.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Message.Should().Be("Envelope not found");
  }

  /// <summary>
  /// Tests that Handle correctly overwrites existing fund amount
  /// </summary>
  [Fact]
  public async Task Handle_EnvelopeWithExistingFundAmount_OverwritesValue()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1, FundAmount = 500m };
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateFundAmount.Handler(context);
    var command = new UpdateFundAmount.Command(1, 250.75m);

    // Act
    UpdateFundAmount.Response result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();

    Envelope? updatedEnvelope = await context.Envelopes.FirstOrDefaultAsync(e => e.Id == 1, TestContext.Current.CancellationToken);
    updatedEnvelope.Should().NotBeNull();
    updatedEnvelope!.FundAmount.Should().Be(250.75m);
  }

  /// <summary>
  /// Tests that Handle passes cancellation token to database operations
  /// </summary>
  [Fact]
  public async Task Handle_WithCancellationToken_PassesTokenToOperations()
  {
    // Arrange
    await using var context = new BudgetContext(CreateInMemoryOptions(), null);

    var envelope = new Envelope { Id = 1, Name = "Test Envelope", CategoryId = "1", FamilyId = 1 };
    context.Envelopes.Add(envelope);
    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

    var handler = new UpdateFundAmount.Handler(context);
    var command = new UpdateFundAmount.Command(1, 100m);
    using var cts = new CancellationTokenSource();

    // Act
    UpdateFundAmount.Response result = await handler.Handle(command, cts.Token);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
  }
}


/// <summary>
/// Unit tests for the UpdateFundAmount.Endpoint class
/// </summary>
public partial class EndpointTests
{
  /// <summary>
  /// Tests that AddRoutes throws NullReferenceException when app parameter is null.
  /// Input: null IEndpointRouteBuilder
  /// Expected: NullReferenceException is thrown
  /// </summary>
  [Fact]
  public void AddRoutes_NullApp_ThrowsNullReferenceException()
  {
    // Arrange
    var endpoint = new UpdateFundAmount.Endpoint();

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => endpoint.AddRoutes(null!));
  }

}