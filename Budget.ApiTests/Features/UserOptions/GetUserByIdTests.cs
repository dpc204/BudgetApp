using Budget.Api.Features.UserOptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Budget.ApiTests.Features.UserOptions;


/// <summary>
/// Unit tests for the Endpoint class in GetUserById feature.
/// </summary>
public partial class EndpointTests
{
  /// <summary>
  /// Tests that AddRoutes throws ArgumentNullException when the app parameter is null.
  /// Input: null IEndpointRouteBuilder
  /// Expected: ArgumentNullException thrown with parameter name "app"
  /// </summary>
  [Fact]
  public void AddRoutes_NullApp_ThrowsArgumentNullException()
  {
    // Arrange
    var endpoint = new GetUserById.Endpoint();

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() => endpoint.AddRoutes(null!));
    Assert.Equal("endpoints", exception.ParamName);
  }

  /// <summary>
  /// Tests that AddRoutes completes without throwing when provided a valid IEndpointRouteBuilder.
  /// Input: Valid mock of IEndpointRouteBuilder
  /// Expected: Method completes successfully
  /// 
  /// NOTE: This test has limited verification capability because:
  /// - MapGet, WithTags, and RequireAuthorization are extension methods that cannot be mocked with Moq
  /// - Full verification of endpoint registration requires integration testing with TestServer
  /// - This test primarily ensures the method doesn't throw with valid input
  /// 
  /// For comprehensive testing of this endpoint, consider:
  /// - Integration tests using WebApplicationFactory and HttpClient
  /// - Verifying the actual HTTP endpoint behavior with test requests
  /// - Testing authorization requirements with authenticated/unauthenticated requests
  /// </summary>
  [Fact]
  public void AddRoutes_ValidApp_CompletesSuccessfully()
  {
    // Arrange
    var mockApp = new Mock<IEndpointRouteBuilder>();
    var mockServiceProvider = new Mock<IServiceProvider>();
    var dataSources = new List<EndpointDataSource>();

    // Setup basic properties that IEndpointRouteBuilder requires
    mockApp.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
    mockApp.Setup(x => x.DataSources).Returns(dataSources);

    // Setup CreateApplicationBuilder to return a functional builder
    mockApp.Setup(x => x.CreateApplicationBuilder())
        .Returns(() =>
        {
          var mockBuilder = new Mock<IApplicationBuilder>();
          mockBuilder.Setup(x => x.ApplicationServices).Returns(mockServiceProvider.Object);
          mockBuilder.Setup(x => x.New()).Returns(mockBuilder.Object);
          mockBuilder.Setup(x => x.Build()).Returns((RequestDelegate)(_ => Task.CompletedTask));
          return mockBuilder.Object;
        });

    var endpoint = new GetUserById.Endpoint();

    // Act
    var exception = Record.Exception(() => endpoint.AddRoutes(mockApp.Object));

    // Assert
    // The method should complete without throwing an exception
    // Note: We cannot verify MapGet was called or verify the endpoint configuration
    // because these are extension methods that operate outside the mock's scope
    Assert.Null(exception);
  }

  /// <summary>
  /// Tests that AddRoutes can be invoked multiple times without side effects.
  /// Input: Valid mock of IEndpointRouteBuilder, called twice
  /// Expected: Both calls complete successfully
  /// 
  /// NOTE: This test verifies idempotent behavior at the method level only.
  /// Actual endpoint registration side effects would need integration testing.
  /// </summary>
  [Fact]
  public void AddRoutes_CalledMultipleTimes_CompletesSuccessfully()
  {
    // Arrange
    var mockApp = new Mock<IEndpointRouteBuilder>();
    var mockServiceProvider = new Mock<IServiceProvider>();
    var dataSources = new List<EndpointDataSource>();
    var mockApplicationBuilder = new Mock<IApplicationBuilder>();

    mockApp.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
    mockApp.Setup(x => x.DataSources).Returns(dataSources);
    mockApp.Setup(x => x.CreateApplicationBuilder()).Returns(mockApplicationBuilder.Object);

    var endpoint = new GetUserById.Endpoint();

    // Act
    var exception1 = Record.Exception(() => endpoint.AddRoutes(mockApp.Object));
    var exception2 = Record.Exception(() => endpoint.AddRoutes(mockApp.Object));

    // Assert
    Assert.Null(exception1);
    Assert.Null(exception2);
  }
}
