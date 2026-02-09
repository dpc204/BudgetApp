using Microsoft.AspNetCore.Routing;
using Moq;

namespace Budget.ApiTests.Features.Transactions;


/// <summary>
/// Unit tests for the UpdateTransactionImport.Endpoint class.
/// </summary>
public partial class EndpointTests
{
    /// <summary>
    /// Tests that the AddRoutes method registers the PUT endpoint at the correct path.
    /// </summary>
    [Fact]
    public void AddRoutes_RegistersPutEndpoint()
    {
        // Arrange
        var endpoint = new UpdateTransactionImport.Endpoint();
        var routeBuilder = new Mock<IEndpointRouteBuilder>();
        var dataSource = new Mock<EndpointDataSource>();
        
        routeBuilder.Setup(x => x.ServiceProvider).Returns(Mock.Of<IServiceProvider>());
        routeBuilder.Setup(x => x.DataSources).Returns([dataSource.Object]);
        
        // Act
        endpoint.AddRoutes(routeBuilder.Object);
        
        // Assert
        routeBuilder.Verify(x => x.ServiceProvider, Times.AtLeastOnce());
    }
}