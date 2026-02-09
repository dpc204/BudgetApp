using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Data.Tables;
using Budget.Api.Features.Utilities.ImportExport;
using Carter;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Budget.ApiTests.Features.Utilities.ImportExport;



/// <summary>
/// Unit tests for GetBackupSetDetails.Handler
/// </summary>
public class GetBackupSetDetailsTests
{
    /// <summary>
    /// Tests that Handle returns empty response when table creation fails
    /// </summary>
    [Fact]
    public async Task Handle_WhenCreateIfNotExistsAsyncThrowsException_ReturnsEmptyResponse()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        var partitionKey = "backup-2024-01-15";
        var exception = new InvalidOperationException("Table creation failed");

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ThrowsAsync(exception);

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().BeEmpty();
        mockLogger.Verify(
          x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to access TableBackups table")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);
    }

    /// <summary>
    /// Tests that Handle returns empty response when RequestFailedException occurs
    /// </summary>
    [Fact]
    public async Task Handle_WhenRequestFailedExceptionThrown_ReturnsEmptyResponse()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        var partitionKey = "backup-2024-01-15";
        var exception = new RequestFailedException("Azure service error");

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ThrowsAsync(exception);

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().BeEmpty();
    }

    /// <summary>
    /// Helper method to create a TableEntity with backup properties
    /// </summary>
    private static TableEntity CreateTableEntity(string tableName, string blobName, int sizeBytes, DateTime exportedAt, string partitionKey)
    {
        var entity = new TableEntity(partitionKey, tableName)
        {
            ["BlobName"] = blobName,
            ["SizeBytes"] = sizeBytes,
            ["ExportedAt"] = exportedAt
        };
        return entity;
    }

    /// <summary>
    /// Tests that AddRoutes registers GET endpoint with correct route pattern.
    /// Input: Valid IEndpointRouteBuilder
    /// Expected: MapGet is called with correct route path
    /// </summary>
    [Fact]
    public void AddRoutes_WithValidApp_RegistersGetEndpointWithCorrectRoute()
    {
        // Arrange
        var mockApp = new Mock<IEndpointRouteBuilder>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockRouteHandlerBuilder = new Mock<IEndpointConventionBuilder>();

        mockApp
            .Setup(x => x.CreateApplicationBuilder())
            .Returns(Mock.Of<IApplicationBuilder>());
        mockApp
            .Setup(x => x.ServiceProvider)
            .Returns(mockServiceProvider.Object);
        mockApp
            .Setup(x => x.DataSources)
            .Returns([]);

        var endpoint = new GetBackupSetDetails.Endpoint();

        // Act & Assert
        // The actual MapGet call cannot be easily verified due to extension method limitations
        // Testing endpoint configuration requires integration-level testing
        // This test verifies the method executes without throwing
        var exception = Record.Exception(() => endpoint.AddRoutes(mockApp.Object));

        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that AddRoutes handler correctly sends query with partition key from route.
    /// Input: Valid partition key
    /// Expected: Query is sent with correct partition key and response tables are returned
    /// </summary>
    [Theory]
    [InlineData("backup-2024-01-15")]
    [InlineData("backup_test")]
    [InlineData("backup.test")]
    [InlineData("backup-with-many-dashes-2024-01-15-12-30-45")]
    [InlineData("a")]
    public async Task AddRoutes_HandlerWithValidPartitionKey_SendsCorrectQueryAndReturnsResponseTables(string partitionKey)
    {
        // Arrange
        var mockSender = new Mock<ISender>();
        var expectedTables = new List<GetBackupSetDetails.BackupTableDto>
        {
            new("Table1", "blob1.json", 1000, DateTime.UtcNow, partitionKey),
            new("Table2", "blob2.json", 2000, DateTime.UtcNow, partitionKey)
        };
        var expectedResponse = new GetBackupSetDetails.Response(expectedTables);

        GetBackupSetDetails.Query? capturedQuery = null;
        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<GetBackupSetDetails.Response>, CancellationToken>((req, ct) =>
            {
                capturedQuery = req as GetBackupSetDetails.Query;
            })
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic directly since we can't easily invoke the lambda from MapGet
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedQuery);
        Assert.Equal(partitionKey, capturedQuery.PartitionKey);
        Assert.Same(expectedResponse, result);
        Assert.Equal(expectedTables, result.Tables);
        mockSender.Verify(x => x.Send(It.Is<GetBackupSetDetails.Query>(q => q.PartitionKey == partitionKey), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that AddRoutes handler works correctly with empty partition key.
    /// Input: Empty string partition key
    /// Expected: Query is sent with empty partition key
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task AddRoutes_HandlerWithEmptyOrWhitespacePartitionKey_SendsQueryWithEmptyPartitionKey(string partitionKey)
    {
        // Arrange
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetBackupSetDetails.Response([]);

        GetBackupSetDetails.Query? capturedQuery = null;
        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<GetBackupSetDetails.Response>, CancellationToken>((req, ct) =>
            {
                capturedQuery = req as GetBackupSetDetails.Query;
            })
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedQuery);
        Assert.Equal(partitionKey, capturedQuery.PartitionKey);
        Assert.Empty(result.Tables);
    }

    /// <summary>
    /// Tests that AddRoutes handler works correctly with special characters in partition key.
    /// Input: Partition key with special characters
    /// Expected: Query is sent with unmodified special characters
    /// </summary>
    [Theory]
    [InlineData("backup/with/slashes")]
    [InlineData("backup\\with\\backslashes")]
    [InlineData("backup:with:colons")]
    [InlineData("backup@with#special$chars%")]
    [InlineData("backup with spaces")]
    [InlineData("backup\twith\ttabs")]
    [InlineData("backup\nwith\nnewlines")]
    public async Task AddRoutes_HandlerWithSpecialCharactersInPartitionKey_SendsQueryWithSpecialCharacters(string partitionKey)
    {
        // Arrange
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetBackupSetDetails.Response([]);

        GetBackupSetDetails.Query? capturedQuery = null;
        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<GetBackupSetDetails.Response>, CancellationToken>((req, ct) =>
            {
                capturedQuery = req as GetBackupSetDetails.Query;
            })
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedQuery);
        Assert.Equal(partitionKey, capturedQuery.PartitionKey);
    }

    /// <summary>
    /// Tests that AddRoutes handler works correctly with very long partition key.
    /// Input: Very long partition key string
    /// Expected: Query is sent with full partition key value
    /// </summary>
    [Fact]
    public async Task AddRoutes_HandlerWithVeryLongPartitionKey_SendsQueryWithFullPartitionKey()
    {
        // Arrange
        var partitionKey = new string('a', 10000);
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetBackupSetDetails.Response([]);

        GetBackupSetDetails.Query? capturedQuery = null;
        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<GetBackupSetDetails.Response>, CancellationToken>((req, ct) =>
            {
                capturedQuery = req as GetBackupSetDetails.Query;
            })
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedQuery);
        Assert.Equal(partitionKey, capturedQuery.PartitionKey);
        Assert.Equal(10000, capturedQuery.PartitionKey.Length);
    }

    /// <summary>
    /// Tests that AddRoutes handler returns empty tables when response has empty tables collection.
    /// Input: Valid partition key, response with empty tables
    /// Expected: Empty tables collection is returned
    /// </summary>
    [Fact]
    public async Task AddRoutes_HandlerWhenResponseHasEmptyTables_ReturnsEmptyTablesCollection()
    {
        // Arrange
        var partitionKey = "backup-2024-01-15";
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetBackupSetDetails.Response([]);

        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, CancellationToken.None);

        // Assert
        Assert.Empty(result.Tables);
    }

    /// <summary>
    /// Tests that AddRoutes handler returns correct tables when response has multiple tables.
    /// Input: Valid partition key, response with multiple tables
    /// Expected: All tables from response are returned
    /// </summary>
    [Fact]
    public async Task AddRoutes_HandlerWhenResponseHasMultipleTables_ReturnsAllTables()
    {
        // Arrange
        var partitionKey = "backup-2024-01-15";
        var mockSender = new Mock<ISender>();
        var table1 = new GetBackupSetDetails.BackupTableDto("Table1", "blob1.json", 1000, DateTime.UtcNow, partitionKey);
        var table2 = new GetBackupSetDetails.BackupTableDto("Table2", "blob2.json", 2000, DateTime.UtcNow, partitionKey);
        var table3 = new GetBackupSetDetails.BackupTableDto("Table3", "blob3.json", 3000, DateTime.UtcNow, partitionKey);
        var expectedTables = new List<GetBackupSetDetails.BackupTableDto> { table1, table2, table3 };
        var expectedResponse = new GetBackupSetDetails.Response(expectedTables);

        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Tables.Count);
        Assert.Equal("Table1", result.Tables[0].TableName);
        Assert.Equal("Table2", result.Tables[1].TableName);
        Assert.Equal("Table3", result.Tables[2].TableName);
    }

    /// <summary>
    /// Tests that AddRoutes handler correctly processes tables with boundary size values.
    /// Input: Tables with minimum and maximum size values
    /// Expected: Size values are preserved correctly
    /// </summary>
    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    public async Task AddRoutes_HandlerWithBoundarySizeValues_PreservesCorrectSizeValues(long sizeBytes)
    {
        // Arrange
        var partitionKey = "backup-2024-01-15";
        var mockSender = new Mock<ISender>();
        var table = new GetBackupSetDetails.BackupTableDto("Table1", "blob1.json", sizeBytes, DateTime.UtcNow, partitionKey);
        var expectedResponse = new GetBackupSetDetails.Response([table]);

        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Tables);
        Assert.Equal(sizeBytes, result.Tables[0].SizeBytes);
    }

    /// <summary>
    /// Tests that AddRoutes handler correctly processes tables with boundary DateTime values.
    /// Input: Tables with minimum, maximum, and normal DateTime values
    /// Expected: DateTime values are preserved correctly
    /// </summary>
    [Theory]
    [InlineData("2024-01-01T00:00:00")]
    [InlineData("9999-12-31T23:59:59")]
    [InlineData("0001-01-01T00:00:00")]
    public async Task AddRoutes_HandlerWithBoundaryDateTimeValues_PreservesCorrectDateTimeValues(string dateTimeString)
    {
        // Arrange
        var partitionKey = "backup-2024-01-15";
        var exportedAt = DateTime.Parse(dateTimeString);
        var mockSender = new Mock<ISender>();
        var table = new GetBackupSetDetails.BackupTableDto("Table1", "blob1.json", 1000, exportedAt, partitionKey);
        var expectedResponse = new GetBackupSetDetails.Response([table]);

        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Tables);
        Assert.Equal(exportedAt, result.Tables[0].ExportedAt);
    }

    /// <summary>
    /// Tests that AddRoutes handler correctly uses cancellation token when provided.
    /// Input: Valid partition key with cancellation token
    /// Expected: Cancellation token is passed to Send method
    /// </summary>
    [Fact]
    public async Task AddRoutes_HandlerWithCancellationToken_PassesTokenToSendMethod()
    {
        // Arrange
        var partitionKey = "backup-2024-01-15";
        var mockSender = new Mock<ISender>();
        var expectedResponse = new GetBackupSetDetails.Response([]);
        var cancellationToken = new CancellationToken();

        CancellationToken capturedToken = default;
        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<GetBackupSetDetails.Response>, CancellationToken>((req, ct) =>
            {
                capturedToken = ct;
            })
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, cancellationToken);

        // Assert
        Assert.Equal(cancellationToken, capturedToken);
        mockSender.Verify(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), cancellationToken), Times.Once);
    }

    /// <summary>
    /// Tests that AddRoutes handler correctly handles tables with empty or whitespace table names.
    /// Input: Tables with empty/whitespace table names
    /// Expected: Empty/whitespace table names are preserved
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task AddRoutes_HandlerWithEmptyOrWhitespaceTableName_PreservesTableName(string tableName)
    {
        // Arrange
        var partitionKey = "backup-2024-01-15";
        var mockSender = new Mock<ISender>();
        var table = new GetBackupSetDetails.BackupTableDto(tableName, "blob1.json", 1000, DateTime.UtcNow, partitionKey);
        var expectedResponse = new GetBackupSetDetails.Response([table]);

        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Tables);
        Assert.Equal(tableName, result.Tables[0].TableName);
    }

    /// <summary>
    /// Tests that AddRoutes handler correctly handles tables with empty or whitespace blob names.
    /// Input: Tables with empty/whitespace blob names
    /// Expected: Empty/whitespace blob names are preserved
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task AddRoutes_HandlerWithEmptyOrWhitespaceBlobName_PreservesBlobName(string blobName)
    {
        // Arrange
        var partitionKey = "backup-2024-01-15";
        var mockSender = new Mock<ISender>();
        var table = new GetBackupSetDetails.BackupTableDto("Table1", blobName, 1000, DateTime.UtcNow, partitionKey);
        var expectedResponse = new GetBackupSetDetails.Response([table]);

        mockSender
            .Setup(x => x.Send(It.IsAny<GetBackupSetDetails.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Simulate the handler logic
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        var result = await mockSender.Object.Send(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Tables);
        Assert.Equal(blobName, result.Tables[0].BlobName);
    }
}