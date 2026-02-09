using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Storage.Blobs;
using Budget.Api.Features.Utilities.ImportExport;
using Carter;
using Fantum.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Budget.Api.Features.Utilities.ImportExport.UnitTests;


/// <summary>
/// Unit tests for DeleteBackupSet.Handler
/// </summary>
public class DeleteBackupSetHandlerTests
{
    /// <summary>
    /// Tests that Handle successfully deletes all entities and blobs when backup set exists
    /// with valid BlobName properties.
    /// Expected: Returns success response with correct count of deleted entities.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidBackupSet_ReturnsSuccessResponse()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();

        var partitionKey = "backup-2024-01-01";
        var entities = new List<TableEntity>
    {
      new(partitionKey, "row1") { { "BlobName", "blob1.json" } },
      new(partitionKey, "row2") { { "BlobName", "blob2.json" } }
    };

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(entities));
        mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(x => x.DeleteIfExistsAsync(
            It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
            It.IsAny<Azure.Storage.Blobs.Models.BlobRequestConditions>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<bool>>());
        mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ETag>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response>());

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("Successfully deleted backup set with 2 tables", result.Message);
    }

    /// <summary>
    /// Tests that Handle returns failure when table creation throws an exception.
    /// Expected: Returns failure response with message "Failed to access backup table".
    /// </summary>
    [Fact]
    public async Task Handle_WhenTableCreationFails_ReturnsFailureResponse()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = "backup-2024-01-01";

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ThrowsAsync(new Exception("Table service unavailable"));

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Failed to access backup table", result.Message);
    }

    /// <summary>
    /// Tests that Handle returns failure when no entities are found for the given PartitionKey.
    /// Expected: Returns failure response with message "Backup set not found".
    /// </summary>
    [Fact]
    public async Task Handle_WhenNoEntitiesFound_ReturnsBackupSetNotFoundResponse()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = "nonexistent-backup";
        var emptyEntities = new List<TableEntity>();

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(emptyEntities));

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Backup set not found", result.Message);
    }

    /// <summary>
    /// Tests that Handle correctly processes entities with null or empty BlobName properties.
    /// Expected: Skips blob deletion for entities without BlobName, deletes entities successfully.
    /// </summary>
    [Fact]
    public async Task Handle_WithNullOrEmptyBlobNames_SkipsBlobDeletionAndDeletesEntities()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = "backup-2024-01-01";
        var entities = new List<TableEntity>
    {
      new(partitionKey, "row1") { { "BlobName", "" } },
      new(partitionKey, "row2"),
      new(partitionKey, "row3") { { "BlobName", "blob3.json" } }
    };

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(entities));

        var mockBlobClient = new Mock<BlobClient>();
        mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(x => x.DeleteIfExistsAsync(
            It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
            It.IsAny<Azure.Storage.Blobs.Models.BlobRequestConditions>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<bool>>());
        mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ETag>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response>());

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("Successfully deleted backup set with 3 tables", result.Message);
        mockBlobClient.Verify(x => x.DeleteIfExistsAsync(
          It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
          It.IsAny<Azure.Storage.Blobs.Models.BlobRequestConditions>(),
          It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that Handle returns partial deletion response when some blob deletions fail.
    /// Expected: Returns failure response indicating partial deletion with failure count.
    /// </summary>
    [Fact]
    public async Task Handle_WhenBlobDeletionFails_ReturnsPartialDeletionResponse()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = "backup-2024-01-01";
        var entities = new List<TableEntity>
    {
      new(partitionKey, "row1") { { "BlobName", "blob1.json" } },
      new(partitionKey, "row2") { { "BlobName", "blob2.json" } }
    };

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(entities));

        var mockBlobClient = new Mock<BlobClient>();
        mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(x => x.DeleteIfExistsAsync(
            It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
            It.IsAny<Azure.Storage.Blobs.Models.BlobRequestConditions>(),
            It.IsAny<CancellationToken>()))
          .ThrowsAsync(new Exception("Blob not accessible"));
        mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ETag>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response>());

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Partial deletion", result.Message);
        Assert.Contains("2 entities", result.Message);
        Assert.Contains("2 failures", result.Message);
    }

    /// <summary>
    /// Tests that Handle returns partial deletion response when some entity deletions fail.
    /// Expected: Returns failure response indicating partial deletion with failure count.
    /// </summary>
    [Fact]
    public async Task Handle_WhenEntityDeletionFails_ReturnsPartialDeletionResponse()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = "backup-2024-01-01";
        var entities = new List<TableEntity>
    {
      new(partitionKey, "row1") { { "BlobName", "blob1.json" } },
      new(partitionKey, "row2") { { "BlobName", "blob2.json" } }
    };

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(entities));

        var mockBlobClient = new Mock<BlobClient>();
        mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(x => x.DeleteIfExistsAsync(
            It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
            It.IsAny<Azure.Storage.Blobs.Models.BlobRequestConditions>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<bool>>());
        mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ETag>(),
            It.IsAny<CancellationToken>()))
          .ThrowsAsync(new Exception("Entity locked"));

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Partial deletion", result.Message);
        Assert.Contains("2 blobs deleted", result.Message);
        Assert.Contains("2 failures", result.Message);
    }

    /// <summary>
    /// Tests that Handle returns partial deletion response when both blob and entity deletions fail.
    /// Expected: Returns failure response with combined failure count.
    /// </summary>
    [Fact]
    public async Task Handle_WhenBothBlobAndEntityDeletionsFail_ReturnsPartialDeletionResponse()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = "backup-2024-01-01";
        var entities = new List<TableEntity>
    {
      new(partitionKey, "row1") { { "BlobName", "blob1.json" } },
      new(partitionKey, "row2") { { "BlobName", "blob2.json" } }
    };

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(entities));

        var mockBlobClient = new Mock<BlobClient>();
        mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(x => x.DeleteIfExistsAsync(
            It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
            It.IsAny<Azure.Storage.Blobs.Models.BlobRequestConditions>(),
            It.IsAny<CancellationToken>()))
          .ThrowsAsync(new Exception("Blob error"));
        mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ETag>(),
            It.IsAny<CancellationToken>()))
          .ThrowsAsync(new Exception("Entity error"));

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Partial deletion", result.Message);
        Assert.Contains("4 failures", result.Message);
    }

    /// <summary>
    /// Tests that Handle catches and returns error response when unexpected exception occurs.
    /// Expected: Returns failure response with exception message.
    /// </summary>
    [Fact]
    public async Task Handle_WhenUnexpectedExceptionOccurs_ReturnsErrorResponse()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();

        var partitionKey = "backup-2024-01-01";
        var exceptionMessage = "Unexpected database error";

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups"))
          .Throws(new Exception(exceptionMessage));

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains($"Error: {exceptionMessage}", result.Message);
    }

    /// <summary>
    /// Tests that Handle processes empty PartitionKey parameter.
    /// Expected: Processes normally (filter will match entities with empty PartitionKey).
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyPartitionKey_ProcessesNormally()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = "";
        var emptyEntities = new List<TableEntity>();

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(emptyEntities));

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Backup set not found", result.Message);
    }

    /// <summary>
    /// Tests that Handle processes PartitionKey with special characters correctly.
    /// Expected: Processes normally with special characters in filter string.
    /// </summary>
    [Fact]
    public async Task Handle_WithSpecialCharactersInPartitionKey_ProcessesNormally()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = "backup-2024-01-01'OR'1'='1";
        var entities = new List<TableEntity>
    {
      new(partitionKey, "row1") { { "BlobName", "blob1.json" } }
    };

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(entities));

        var mockBlobClient = new Mock<BlobClient>();
        mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(x => x.DeleteIfExistsAsync(
            It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
            It.IsAny<Azure.Storage.Blobs.Models.BlobRequestConditions>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<bool>>());
        mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ETag>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response>());

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    /// <summary>
    /// Tests that Handle processes very long PartitionKey values.
    /// Expected: Processes normally without truncation or errors.
    /// </summary>
    [Fact]
    public async Task Handle_WithVeryLongPartitionKey_ProcessesNormally()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = new string('a', 1000);
        var emptyEntities = new List<TableEntity>();

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(emptyEntities));

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Backup set not found", result.Message);
    }

    /// <summary>
    /// Tests that Handle correctly handles mixed success and failure in blob deletions.
    /// Expected: Returns partial deletion response with accurate counts.
    /// </summary>
    [Fact]
    public async Task Handle_WithMixedBlobDeletionResults_ReturnsPartialDeletionResponse()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = "backup-2024-01-01";
        var entities = new List<TableEntity>
    {
      new(partitionKey, "row1") { { "BlobName", "blob1.json" } },
      new(partitionKey, "row2") { { "BlobName", "blob2.json" } },
      new(partitionKey, "row3") { { "BlobName", "blob3.json" } }
    };

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(entities));

        var callCount = 0;
        var mockBlobClient = new Mock<BlobClient>();
        mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(x => x.DeleteIfExistsAsync(
            It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
            It.IsAny<Azure.Storage.Blobs.Models.BlobRequestConditions>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(() =>
          {
              callCount++;
              if (callCount == 2)
              {
                  throw new Exception("Blob 2 failed");
              }
              return Mock.Of<Response<bool>>();
          });
        mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ETag>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response>());

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Partial deletion", result.Message);
        Assert.Contains("2 blobs deleted", result.Message);
        Assert.Contains("3 entities", result.Message);
    }

    /// <summary>
    /// Tests that Handle correctly handles mixed success and failure in entity deletions.
    /// Expected: Returns partial deletion response with accurate counts.
    /// </summary>
    [Fact]
    public async Task Handle_WithMixedEntityDeletionResults_ReturnsPartialDeletionResponse()
    {
        // Arrange
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockLogger = new Mock<ILogger<DeleteBackupSet.Handler>>();
        var mockTableClient = new Mock<TableClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        var partitionKey = "backup-2024-01-01";
        var entities = new List<TableEntity>
    {
      new(partitionKey, "row1") { { "BlobName", "blob1.json" } },
      new(partitionKey, "row2") { { "BlobName", "blob2.json" } },
      new(partitionKey, "row3") { { "BlobName", "blob3.json" } }
    };

        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient("backups")).Returns(mockBlobContainerClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()))
          .Returns(new TestAsyncPageable<TableEntity>(entities));

        var mockBlobClient = new Mock<BlobClient>();
        mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(x => x.DeleteIfExistsAsync(
            It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
            It.IsAny<Azure.Storage.Blobs.Models.BlobRequestConditions>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(Mock.Of<Response<bool>>());

        var entityCallCount = 0;
        mockTableClient.Setup(x => x.DeleteEntityAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ETag>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(() =>
          {
              entityCallCount++;
              if (entityCallCount == 2)
              {
                  throw new Exception("Entity 2 failed");
              }
              return Mock.Of<Response>();
          });

        var handler = new DeleteBackupSet.Handler(
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockLogger.Object);

        var command = new DeleteBackupSet.Command(partitionKey);

        // Act
        DeleteBackupSet.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Partial deletion", result.Message);
        Assert.Contains("2 entities", result.Message);
        Assert.Contains("3 blobs deleted", result.Message);
    }

    /// <summary>
    /// Test helper class to create an AsyncPageable from a list of entities.
    /// Enables testing of async enumerable behavior without complex mocking.
    /// </summary>
    private class TestAsyncPageable<T> : AsyncPageable<T>
    {
        private readonly IEnumerable<T> _items;

        public TestAsyncPageable(IEnumerable<T> items)
        {
            _items = items ?? Enumerable.Empty<T>();
        }

        public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            await Task.Yield();
            yield return Page<T>.FromValues(_items.ToList(), null, Mock.Of<Response>());
        }
    }
}
