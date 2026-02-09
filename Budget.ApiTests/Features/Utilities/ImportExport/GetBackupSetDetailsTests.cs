using Azure;
using Azure.Data.Tables;
using Budget.Api.Features.Utilities.ImportExport;
using Microsoft.Extensions.Logging;
using Moq;

namespace Budget.ApiTests.Features.Utilities.ImportExport;


/// <summary>
/// Unit tests for GetBackupSetDetails.Handler
/// </summary>
public class GetBackupSetDetailsTests
{
    /// <summary>
    /// Tests that Handle returns sorted backup table details when entities exist for the partition key
    /// </summary>
    [Fact]
    public async Task Handle_WithValidPartitionKeyAndMultipleEntities_ReturnsSortedBackupTableDetails()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        var partitionKey = "backup-2024-01-15";
        var entities = new List<TableEntity>
    {
      CreateTableEntity("TableC", "blob-c.json", 3000, new DateTime(2024, 1, 15, 10, 0, 0), partitionKey),
      CreateTableEntity("TableA", "blob-a.json", 1000, new DateTime(2024, 1, 15, 10, 0, 0), partitionKey),
      CreateTableEntity("TableB", "blob-b.json", 2000, new DateTime(2024, 1, 15, 10, 0, 0), partitionKey)
    };

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(entities));

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

    // Act
    GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().HaveCount(3);
        result.Tables[0].TableName.Should().Be("TableA");
        result.Tables[1].TableName.Should().Be("TableB");
        result.Tables[2].TableName.Should().Be("TableC");
        result.Tables.All(t => t.PartitionKey == partitionKey).Should().BeTrue();
    }

    /// <summary>
    /// Tests that Handle returns empty response when no entities match the partition key
    /// </summary>
    [Fact]
    public async Task Handle_WithPartitionKeyHavingNoEntities_ReturnsEmptyResponse()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        var partitionKey = "backup-nonexistent";

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>([]));

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

    // Act
    GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().BeEmpty();
    }

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
    /// Tests that Handle correctly maps entity properties with null values to defaults
    /// </summary>
    [Fact]
    public async Task Handle_WithEntityHavingNullProperties_UsesDefaultValues()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        var partitionKey = "backup-2024-01-15";
        var entity = new TableEntity(partitionKey, "TestTable");
        // Not setting BlobName, SizeBytes, or ExportedAt

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>([entity]));

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

    // Act
    GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().HaveCount(1);
        result.Tables[0].TableName.Should().Be("TestTable");
        result.Tables[0].BlobName.Should().Be(string.Empty);
        result.Tables[0].SizeBytes.Should().Be(0);
        result.Tables[0].ExportedAt.Should().Be(DateTime.MinValue);
    }

    /// <summary>
    /// Tests that Handle correctly processes single entity
    /// </summary>
    [Fact]
    public async Task Handle_WithSingleEntity_ReturnsOneBackupTableDto()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        var partitionKey = "backup-2024-01-15";
    TableEntity entity = CreateTableEntity("SingleTable", "single-blob.json", 5000, new DateTime(2024, 1, 15, 12, 0, 0), partitionKey);

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>([entity]));

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

    // Act
    GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().HaveCount(1);
        result.Tables[0].TableName.Should().Be("SingleTable");
        result.Tables[0].BlobName.Should().Be("single-blob.json");
        result.Tables[0].SizeBytes.Should().Be(5000);
        result.Tables[0].ExportedAt.Should().Be(new DateTime(2024, 1, 15, 12, 0, 0));
    }

    /// <summary>
    /// Tests that Handle uses correct OData filter with special characters in partition key
    /// </summary>
    [Theory]
    [InlineData("backup-2024-01-15")]
    [InlineData("backup_test")]
    [InlineData("backup.test")]
    [InlineData("backup-with-many-dashes-2024-01-15-12-30-45")]
    [InlineData("a")]
    public async Task Handle_WithVariousPartitionKeyFormats_QueriesWithCorrectFilter(string partitionKey)
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        string? capturedFilter = null;

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Callback<string, int?, IEnumerable<string>, CancellationToken>((filter, maxPerPage, select, ct) => capturedFilter = filter)
          .Returns(new TestAsyncPageable<TableEntity>([]));

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

    // Act
    GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        capturedFilter.Should().Be($"PartitionKey eq '{partitionKey}'");
    }

    /// <summary>
    /// Tests that Handle processes empty partition key
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Handle_WithEmptyOrWhitespacePartitionKey_ProcessesRequest(string partitionKey)
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>([]));

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

    // Act
    GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle correctly handles large size values
    /// </summary>
    [Fact]
    public async Task Handle_WithLargeSizeBytes_ProcessesCorrectly()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        var partitionKey = "backup-2024-01-15";
    TableEntity entity = CreateTableEntity("LargeTable", "large-blob.json", int.MaxValue, new DateTime(2024, 1, 15, 10, 0, 0), partitionKey);

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>([entity]));

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

    // Act
    GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().HaveCount(1);
        result.Tables[0].SizeBytes.Should().Be(int.MaxValue);
    }

    /// <summary>
    /// Tests that Handle correctly handles boundary DateTime values
    /// </summary>
    [Theory]
    [InlineData("2024-01-01T00:00:00")]
    [InlineData("9999-12-31T23:59:59")]
    [InlineData("0001-01-01T00:00:00")]
    public async Task Handle_WithBoundaryDateTimeValues_ProcessesCorrectly(string dateTimeString)
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        var partitionKey = "backup-2024-01-15";
        var exportedAt = DateTime.Parse(dateTimeString);
    TableEntity entity = CreateTableEntity("TestTable", "test-blob.json", 1000, exportedAt, partitionKey);

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>([entity]));

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

    // Act
    GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().HaveCount(1);
        result.Tables[0].ExportedAt.Should().Be(exportedAt);
    }

    /// <summary>
    /// Tests that Handle logs informational messages correctly
    /// </summary>
    [Fact]
    public async Task Handle_WithValidRequest_LogsInformationalMessages()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        var partitionKey = "backup-2024-01-15";
        var entities = new List<TableEntity>
    {
      CreateTableEntity("Table1", "blob1.json", 1000, new DateTime(2024, 1, 15, 10, 0, 0), partitionKey),
      CreateTableEntity("Table2", "blob2.json", 2000, new DateTime(2024, 1, 15, 10, 0, 0), partitionKey)
    };

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(entities));

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

    // Act
    GetBackupSetDetails.Response result = await handler.Handle(query, CancellationToken.None);

        // Assert
        mockLogger.Verify(
          x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieving backup set details")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);

        mockLogger.Verify(
          x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Found 2 tables")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);
    }

    /// <summary>
    /// Tests that Handle passes cancellation token through to async operations
    /// </summary>
    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToAsyncOperations()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSetDetails.Handler>>();

        var partitionKey = "backup-2024-01-15";
        var cancellationToken = new CancellationToken(canceled: false);
        CancellationToken capturedToken = default;

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .Callback<CancellationToken>(ct => capturedToken = ct)
          .ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>([]));

        var handler = new GetBackupSetDetails.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSetDetails.Query(partitionKey);

        // Act
        await handler.Handle(query, cancellationToken);

        // Assert
        capturedToken.Should().Be(cancellationToken);
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
        var entity = new TableEntity(partitionKey, tableName) {
          ["BlobName"] = blobName,
          ["SizeBytes"] = sizeBytes,
          ["ExportedAt"] = exportedAt
        };
        return entity;
    }

    /// <summary>
    /// Test helper class to create AsyncPageable from a list for testing purposes
    /// </summary>
    private class TestAsyncPageable<T>(IEnumerable<T> items) : AsyncPageable<T> where T : notnull
    {
        private readonly IEnumerable<T> _items = items;

    public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            await Task.Yield();
            yield return new TestPage<T>(_items);
        }
    }

    /// <summary>
    /// Test helper class to create a Page for testing purposes
    /// </summary>
    private class TestPage<T>(IEnumerable<T> items) : Page<T>
    {
        private readonly IEnumerable<T> _items = items;

    public override IReadOnlyList<T> Values => [.. _items];

        public override string? ContinuationToken => null;

        public override Response GetRawResponse() => Mock.Of<Response>();
    }
}



   

  
