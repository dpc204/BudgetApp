using Budget.Api.Features.Admin.Users;
using Budget.DB;
using Carter;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Budget.Api.Features.Admin.Users.UnitTests;


/// <summary>
/// Unit tests for UpdateUser.Endpoint class
/// </summary>
public partial class EndpointTests
{
    /// <summary>
    /// Tests that AddRoutes successfully registers the endpoint without throwing exceptions.
    /// Input: Valid IEndpointRouteBuilder mock
    /// Expected: Method completes without exceptions
    /// </summary>
    /// <remarks>
    /// Note: This test verifies that the AddRoutes method can be invoked without errors.
    /// Comprehensive testing of the actual endpoint behavior (ID validation, sender invocation,
    /// response mapping, authorization, and tagging) requires integration testing using
    /// WebApplicationFactory or TestServer, which is beyond the scope of unit testing.
    /// The endpoint logic includes:
    /// - ID mismatch validation (route id vs command.Id)
    /// - Sending command via ISender
    /// - Response mapping (Ok vs NotFound)
    /// - Admin authorization requirement
    /// - Admin tag assignment
    /// These behaviors should be validated through integration tests.
    /// </remarks>
    [Fact]
    public void AddRoutes_WithValidRouteBuilder_CompletesSuccessfully()
    {
        // Arrange
        var mockRouteBuilder = new Mock<IEndpointRouteBuilder>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockRouteBuilder
            .Setup(x => x.ServiceProvider)
            .Returns(mockServiceProvider.Object);

        mockRouteBuilder
            .Setup(x => x.DataSources)
            .Returns(new List<EndpointDataSource>());

        var endpoint = new UpdateUser.Endpoint();

        // Act & Assert
        var exception = Record.Exception(() => endpoint.AddRoutes(mockRouteBuilder.Object));
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that AddRoutes throws ArgumentNullException when route builder is null.
    /// Input: null IEndpointRouteBuilder
    /// Expected: ArgumentNullException or NullReferenceException
    /// </summary>
    [Fact]
    public void AddRoutes_WithNullRouteBuilder_ThrowsException()
    {
        // Arrange
        var endpoint = new UpdateUser.Endpoint();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => endpoint.AddRoutes(null!));
    }

    /// <summary>
    /// Tests that Command record can be instantiated with valid values.
    /// Input: Valid command parameters
    /// Expected: Command object created successfully with correct property values
    /// </summary>
    [Fact]
    public void Command_WithValidValues_CreatesSuccessfully()
    {
        // Arrange & Act
        var command = new UpdateUser.Command(
            Id: 1,
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            FamilyId: 100);

        // Assert
        Assert.Equal(1, command.Id);
        Assert.Equal("test@example.com", command.Email);
        Assert.Equal("John", command.FirstName);
        Assert.Equal("Doe", command.LastName);
        Assert.Equal(100, command.FamilyId);
    }

    /// <summary>
    /// Tests that Command record can be instantiated with boundary integer values.
    /// Input: int.MinValue and int.MaxValue for Id and FamilyId
    /// Expected: Command object created successfully
    /// </summary>
    [Theory]
    [InlineData(int.MinValue, int.MinValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(0, 0)]
    [InlineData(-1, -1)]
    public void Command_WithBoundaryIntegerValues_CreatesSuccessfully(int id, int familyId)
    {
        // Arrange & Act
        var command = new UpdateUser.Command(
            Id: id,
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            FamilyId: familyId);

        // Assert
        Assert.Equal(id, command.Id);
        Assert.Equal(familyId, command.FamilyId);
    }

    /// <summary>
    /// Tests that Command record can be instantiated with edge case string values.
    /// Input: Empty strings, whitespace, and very long strings
    /// Expected: Command object created successfully (validation happens at handler level)
    /// </summary>
    [Theory]
    [InlineData("", "", "")]
    [InlineData(" ", " ", " ")]
    [InlineData("a", "b", "c")]
    public void Command_WithEdgeCaseStringValues_CreatesSuccessfully(string email, string firstName, string lastName)
    {
        // Arrange & Act
        var command = new UpdateUser.Command(
            Id: 1,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            FamilyId: 1);

        // Assert
        Assert.Equal(email, command.Email);
        Assert.Equal(firstName, command.FirstName);
        Assert.Equal(lastName, command.LastName);
    }

    /// <summary>
    /// Tests that Command record can be instantiated with special characters in strings.
    /// Input: Strings containing special characters and Unicode
    /// Expected: Command object created successfully
    /// </summary>
    [Theory]
    [InlineData("test+tag@example.com", "John's", "O'Brien")]
    [InlineData("user@domain.co.uk", "François", "Müller")]
    [InlineData("test@test.com", "名前", "姓")]
    public void Command_WithSpecialCharacters_CreatesSuccessfully(string email, string firstName, string lastName)
    {
        // Arrange & Act
        var command = new UpdateUser.Command(
            Id: 1,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            FamilyId: 1);

        // Assert
        Assert.Equal(email, command.Email);
        Assert.Equal(firstName, command.FirstName);
        Assert.Equal(lastName, command.LastName);
    }

    /// <summary>
    /// Tests that Response record can be instantiated with valid values.
    /// Input: Valid response parameters
    /// Expected: Response object created successfully with correct property values
    /// </summary>
    [Fact]
    public void Response_WithValidValues_CreatesSuccessfully()
    {
        // Arrange & Act
        var response = new UpdateUser.Response(
            Id: 1,
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            FamilyId: 100);

        // Assert
        Assert.Equal(1, response.Id);
        Assert.Equal("test@example.com", response.Email);
        Assert.Equal("John", response.FirstName);
        Assert.Equal("Doe", response.LastName);
        Assert.Equal(100, response.FamilyId);
    }

    /// <summary>
    /// Tests that Response record can be instantiated with boundary integer values.
    /// Input: int.MinValue and int.MaxValue for Id and FamilyId
    /// Expected: Response object created successfully
    /// </summary>
    [Theory]
    [InlineData(int.MinValue, int.MinValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(0, 0)]
    [InlineData(-1, -1)]
    public void Response_WithBoundaryIntegerValues_CreatesSuccessfully(int id, int familyId)
    {
        // Arrange & Act
        var response = new UpdateUser.Response(
            Id: id,
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            FamilyId: familyId);

        // Assert
        Assert.Equal(id, response.Id);
        Assert.Equal(familyId, response.FamilyId);
    }
}
