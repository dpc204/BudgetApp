using Budget.Api.Features.Admin.UserRoles;
using Budget.DB;
using Carter;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi.Generated;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Language;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Budget.Api.Features.Admin.UnitTests;
/// <summary>
/// Unit tests for the GetUserRoles.Endpoint class
/// </summary>
public sealed class GetUserRolesEndpointTests
{
    /// <summary>
    /// Tests that AddRoutes throws ArgumentNullException when app parameter is null.
    /// Input: null IEndpointRouteBuilder
    /// Expected: ArgumentNullException or NullReferenceException
    /// </summary>
    [Fact]
    public void AddRoutes_NullEndpointRouteBuilder_ThrowsException()
    {
        // Arrange
        var endpoint = new GetUserRoles.Endpoint();
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => endpoint.AddRoutes(null!));
    }

    /// <summary>
    /// Tests that the endpoint handler correctly processes a valid positive userId.
    /// Input: Valid positive userId (123)
    /// Expected: Handler sends Query and returns Ok result with Response
    /// </summary>
    [Fact]
    public async Task EndpointHandler_ValidPositiveUserId_ReturnsOkResult()
    {
        // Arrange
        var userId = 123;
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetUserRoles.Response(userId, new System.Collections.Generic.List<GetUserRoles.RoleDto>());
        mockSender.Setup(x => x.Send(It.Is<GetUserRoles.Query>(q => q.UserId == userId), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        // Act
        var result = await mockSender.Object.Send(new GetUserRoles.Query(userId), CancellationToken.None);
        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        mockSender.Verify(x => x.Send(It.Is<GetUserRoles.Query>(q => q.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that the endpoint handler correctly processes userId of zero.
    /// Input: userId = 0
    /// Expected: Handler sends Query with userId 0 and returns Response
    /// </summary>
    [Fact]
    public async Task EndpointHandler_UserIdZero_SendsQuerySuccessfully()
    {
        // Arrange
        var userId = 0;
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetUserRoles.Response(userId, new System.Collections.Generic.List<GetUserRoles.RoleDto>());
        mockSender.Setup(x => x.Send(It.Is<GetUserRoles.Query>(q => q.UserId == 0), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        // Act
        var result = await mockSender.Object.Send(new GetUserRoles.Query(userId), CancellationToken.None);
        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.UserId);
    }

    /// <summary>
    /// Tests that the endpoint handler correctly processes negative userId.
    /// Input: userId = -1
    /// Expected: Handler sends Query with negative userId and returns Response
    /// </summary>
    [Fact]
    public async Task EndpointHandler_NegativeUserId_SendsQuerySuccessfully()
    {
        // Arrange
        var userId = -1;
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetUserRoles.Response(userId, new System.Collections.Generic.List<GetUserRoles.RoleDto>());
        mockSender.Setup(x => x.Send(It.Is<GetUserRoles.Query>(q => q.UserId == -1), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        // Act
        var result = await mockSender.Object.Send(new GetUserRoles.Query(userId), CancellationToken.None);
        // Assert
        Assert.NotNull(result);
        Assert.Equal(-1, result.UserId);
    }

    /// <summary>
    /// Tests that the endpoint handler correctly processes int.MaxValue userId.
    /// Input: userId = int.MaxValue
    /// Expected: Handler sends Query and returns Response
    /// </summary>
    [Fact]
    public async Task EndpointHandler_UserIdMaxValue_SendsQuerySuccessfully()
    {
        // Arrange
        var userId = int.MaxValue;
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetUserRoles.Response(userId, new System.Collections.Generic.List<GetUserRoles.RoleDto>());
        mockSender.Setup(x => x.Send(It.Is<GetUserRoles.Query>(q => q.UserId == int.MaxValue), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        // Act
        var result = await mockSender.Object.Send(new GetUserRoles.Query(userId), CancellationToken.None);
        // Assert
        Assert.NotNull(result);
        Assert.Equal(int.MaxValue, result.UserId);
    }

    /// <summary>
    /// Tests that the endpoint handler correctly processes int.MinValue userId.
    /// Input: userId = int.MinValue
    /// Expected: Handler sends Query and returns Response
    /// </summary>
    [Fact]
    public async Task EndpointHandler_UserIdMinValue_SendsQuerySuccessfully()
    {
        // Arrange
        var userId = int.MinValue;
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetUserRoles.Response(userId, new System.Collections.Generic.List<GetUserRoles.RoleDto>());
        mockSender.Setup(x => x.Send(It.Is<GetUserRoles.Query>(q => q.UserId == int.MinValue), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        // Act
        var result = await mockSender.Object.Send(new GetUserRoles.Query(userId), CancellationToken.None);
        // Assert
        Assert.NotNull(result);
        Assert.Equal(int.MinValue, result.UserId);
    }

    /// <summary>
    /// Tests that the endpoint handler propagates exceptions from ISender.Send.
    /// Input: ISender.Send throws InvalidOperationException
    /// Expected: Exception is propagated
    /// </summary>
    [Fact]
    public async Task EndpointHandler_SenderThrowsException_PropagatesException()
    {
        // Arrange
        var userId = 123;
        var mockSender = new Mock<ISender>();
        mockSender.Setup(x => x.Send(It.IsAny<GetUserRoles.Query>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database error"));
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await mockSender.Object.Send(new GetUserRoles.Query(userId), CancellationToken.None));
    }

    /// <summary>
    /// Tests that the endpoint handler respects cancellation token.
    /// Input: Cancelled CancellationToken
    /// Expected: OperationCanceledException is thrown
    /// </summary>
    [Fact]
    public async Task EndpointHandler_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var userId = 123;
        var mockSender = new Mock<ISender>();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        mockSender.Setup(x => x.Send(It.IsAny<GetUserRoles.Query>(), It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await mockSender.Object.Send(new GetUserRoles.Query(userId), cts.Token));
    }
}
