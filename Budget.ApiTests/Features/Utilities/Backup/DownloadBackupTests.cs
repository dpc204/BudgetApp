using Budget.Api.Features.Utilities.Backup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Budget.ApiTests.Features.Utilities.Backup;


/// <summary>
/// Unit tests for the DownloadBackup.Endpoint class
/// </summary>
public partial class EndpointTests
{
    /// <summary>
    /// Tests that AddRoutes successfully registers the endpoint without throwing
    /// when provided with a valid IEndpointRouteBuilder.
    /// Input: Valid mocked IEndpointRouteBuilder
    /// Expected: No exception is thrown
    /// </summary>
    [Fact]
    public void AddRoutes_WithValidRouteBuilder_DoesNotThrow()
    {
        // Arrange
        var mockRouteBuilder = new Mock<IEndpointRouteBuilder>();
        var mockRouteHandlerBuilder = new Mock<IEndpointConventionBuilder>();

        mockRouteHandlerBuilder
            .Setup(x => x.Add(It.IsAny<Action<EndpointBuilder>>()))
            .Verifiable();

        mockRouteBuilder
            .Setup(x => x.CreateApplicationBuilder())
            .Returns(Mock.Of<IApplicationBuilder>());

        mockRouteBuilder
            .Setup(x => x.ServiceProvider)
            .Returns(Mock.Of<IServiceProvider>());

        mockRouteBuilder
            .Setup(x => x.DataSources)
            .Returns([]);

        var endpoint = new DownloadBackup.Endpoint();

        // Act
        var exception = Record.Exception(() => endpoint.AddRoutes(mockRouteBuilder.Object));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that AddRoutes throws ArgumentNullException when the app parameter is null.
    /// Input: null IEndpointRouteBuilder
    /// Expected: ArgumentNullException or NullReferenceException
    /// </summary>
    [Fact]
    public void AddRoutes_WithNullRouteBuilder_ThrowsException()
    {
        // Arrange
        var endpoint = new DownloadBackup.Endpoint();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => endpoint.AddRoutes(null!));
    }
}
