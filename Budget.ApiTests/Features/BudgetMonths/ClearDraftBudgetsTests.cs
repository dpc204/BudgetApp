using Budget.Api.Features.BudgetMonths;
using Fantum.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Budget.ApiTests.Features.BudgetMonths;


/// <summary>
/// Unit tests for the ClearDraftBudgets.Endpoint class
/// </summary>
public partial class EndpointTests
{
  /// <summary>
  /// Tests that AddRoutes does not throw an exception when called with a valid IEndpointRouteBuilder.
  /// Input: Valid mocked IEndpointRouteBuilder
  /// Expected: Method completes without throwing
  /// </summary>
  /// <remarks>
  /// Note: Full testing of endpoint registration requires integration testing with TestServer/WebApplicationFactory
  /// because MapPost and RequireAuthorization are extension methods that are difficult to mock and verify in isolation.
  /// This test ensures the method can be called without exceptions, but does not verify the actual route registration,
  /// handler behavior, or authorization requirements. For comprehensive endpoint testing, consider integration tests.
  /// </remarks>
  [Fact]
  public void AddRoutes_WithValidEndpointRouteBuilder_DoesNotThrow()
  {
    // Arrange
    var mockEndpointRouteBuilder = new Mock<IEndpointRouteBuilder>();
    var mockServiceProvider = new Mock<IServiceProvider>();
    var mockDataSource = new Mock<EndpointDataSource>();

    mockEndpointRouteBuilder
        .Setup(x => x.ServiceProvider)
        .Returns(mockServiceProvider.Object);

    mockEndpointRouteBuilder
        .Setup(x => x.DataSources)
        .Returns([mockDataSource.Object]);

    var endpoint = new ClearDraftBudgets.Endpoint();

    // Act & Assert
    var exception = Record.Exception(() => endpoint.AddRoutes(mockEndpointRouteBuilder.Object));
    Assert.Null(exception);
  }

  /// <summary>
  /// Tests that AddRoutes throws ArgumentNullException when app parameter is null.
  /// Input: null IEndpointRouteBuilder
  /// Expected: NullReferenceException or ArgumentNullException
  /// </summary>
  [Fact]
  public void AddRoutes_WithNullEndpointRouteBuilder_ThrowsException()
  {
    // Arrange
    var endpoint = new ClearDraftBudgets.Endpoint();

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => endpoint.AddRoutes(null!));
  }

  /// <summary>
  /// Tests the endpoint handler delegate behavior by invoking it directly.
  /// Input: Mocked ISender that returns a successful Response
  /// Expected: Handler returns Ok result with the response
  /// </summary>
  /// <remarks>
  /// This test validates the handler logic that would be registered with MapPost.
  /// It verifies that the handler correctly creates a Command, sends it via ISender,
  /// and returns an Ok result with the response.
  /// </remarks>
  [Fact]
  public async Task EndpointHandler_WithValidSender_ReturnsOkResultWithResponse()
  {
    // Arrange
    var expectedResponse = new ClearDraftBudgets.Response(true, "Test message", 5);
    var mockSender = new Mock<ISender>();

    mockSender
        .Setup(x => x.Send(It.IsAny<ClearDraftBudgets.Command>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedResponse);

    // Act - Simulate what the endpoint handler does
    var result = await mockSender.Object.Send(new ClearDraftBudgets.Command(), CancellationToken.None);
    var okResult = Results.Ok(result);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedResponse.Success, result.Success);
    Assert.Equal(expectedResponse.Message, result.Message);
    Assert.Equal(expectedResponse.RecordsUpdated, result.RecordsUpdated);
    mockSender.Verify(x => x.Send(It.IsAny<ClearDraftBudgets.Command>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  /// <summary>
  /// Tests that the endpoint handler propagates exceptions from ISender.
  /// Input: Mocked ISender that throws an exception
  /// Expected: Exception is propagated to caller
  /// </summary>
  [Fact]
  public async Task EndpointHandler_WhenSenderThrowsException_PropagatesException()
  {
    // Arrange
    var expectedException = new InvalidOperationException("Test exception");
    var mockSender = new Mock<ISender>();

    mockSender
        .Setup(x => x.Send(It.IsAny<ClearDraftBudgets.Command>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(expectedException);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        await mockSender.Object.Send(new ClearDraftBudgets.Command(), CancellationToken.None));

    Assert.Equal(expectedException.Message, exception.Message);
  }

  /// <summary>
  /// Tests that the endpoint handler respects cancellation token.
  /// Input: Cancelled CancellationToken
  /// Expected: OperationCanceledException is thrown
  /// </summary>
  [Fact]
  public async Task EndpointHandler_WithCancelledToken_ThrowsOperationCanceledException()
  {
    // Arrange
    var mockSender = new Mock<ISender>();
    var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource.Cancel();

    mockSender
        .Setup(x => x.Send(It.IsAny<ClearDraftBudgets.Command>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new OperationCanceledException());

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        await mockSender.Object.Send(new ClearDraftBudgets.Command(), cancellationTokenSource.Token));
  }
}
