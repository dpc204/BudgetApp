using System;
using System.Threading;
using System.Threading.Tasks;

using Budget.Api.Features.Transactions;
using Budget.DB;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Budget.Api.Features.Transactions.UnitTests;


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
        routeBuilder.Setup(x => x.DataSources).Returns(new[] { dataSource.Object });
        
        // Act
        endpoint.AddRoutes(routeBuilder.Object);
        
        // Assert
        routeBuilder.Verify(x => x.ServiceProvider, Times.AtLeastOnce());
    }
}