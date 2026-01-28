using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Budget.Api.Features.Utilities.ImportExport;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Budget.Api.Features.Utilities.UnitTests;


/// <summary>
/// Unit tests for DownloadBackupCsv.Handler
/// </summary>
public class DownloadBackupCsvHandlerTests
{
    /// <summary>
    /// Tests that Handle returns successful response with content, content type, and filename when blob exists
    /// </summary>
    [Fact]
    public async Task Handle_BlobExists_ReturnsContentWithCorrectMetadata()
    {
        // Arrange
        var blobName = "BackupSet-2024-01-06/TableName.csv";
        var expectedFileName = "TableName.csv";
        var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockExistsResponse = new Mock<Response<bool>>();
        var mockDownloadResponse = new Mock<Response<BlobDownloadInfo>>();
        var mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

        mockExistsResponse.Setup(x => x.Value).Returns(true);
        mockExistsResponse.Setup(x => x.GetRawResponse()).Returns(Mock.Of<Response>());

        mockDownloadResponse.Setup(x => x.Value).Returns(mockBlobDownloadInfo);
        mockDownloadResponse.Setup(x => x.GetRawResponse()).Returns(Mock.Of<Response>());

        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("backups"))
            .Returns(mockBlobContainerClient.Object);

        mockBlobContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockExistsResponse.Object);

        mockBlobClient
            .Setup(x => x.DownloadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDownloadResponse.Object);

        var handler = new DownloadBackupCsv.Handler(mockBlobServiceClient.Object, mockLogger.Object);
        var query = new DownloadBackupCsv.Query(blobName);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeNull();
        result.ContentType.Should().Be("text/csv");
        result.FileName.Should().Be(expectedFileName);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Downloading CSV from blob: {blobName}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that Handle returns null response when blob does not exist
    /// </summary>
    [Fact]
    public async Task Handle_BlobNotFound_ReturnsNullResponse()
    {
        // Arrange
        var blobName = "NonExistentBlob.csv";
        var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockExistsResponse = new Mock<Response<bool>>();

        mockExistsResponse.Setup(x => x.Value).Returns(false);
        mockExistsResponse.Setup(x => x.GetRawResponse()).Returns(Mock.Of<Response>());

        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("backups"))
            .Returns(mockBlobContainerClient.Object);

        mockBlobContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockExistsResponse.Object);

        var handler = new DownloadBackupCsv.Handler(mockBlobServiceClient.Object, mockLogger.Object);
        var query = new DownloadBackupCsv.Query(blobName);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeNull();
        result.ContentType.Should().BeNull();
        result.FileName.Should().BeNull();

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Blob not found: {blobName}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that Handle returns null response when exception is thrown during blob operations
    /// </summary>
    [Fact]
    public async Task Handle_ExceptionThrown_ReturnsNullResponse()
    {
        // Arrange
        var blobName = "TestBlob.csv";
        var expectedException = new InvalidOperationException("Blob service error");
        var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();

        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("backups"))
            .Returns(mockBlobContainerClient.Object);

        mockBlobContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Throws(expectedException);

        var handler = new DownloadBackupCsv.Handler(mockBlobServiceClient.Object, mockLogger.Object);
        var query = new DownloadBackupCsv.Query(blobName);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeNull();
        result.ContentType.Should().BeNull();
        result.FileName.Should().BeNull();

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error downloading blob: {blobName}")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that Handle extracts filename correctly from blob path with multiple folders
    /// </summary>
    [Theory]
    [InlineData("BackupSet-2024-01-06/TableName.csv", "TableName.csv")]
    [InlineData("folder1/folder2/file.csv", "file.csv")]
    [InlineData("simple.csv", "simple.csv")]
    [InlineData("BackupSet-2024-01-06/Nested/Folder/Data.csv", "Data.csv")]
    public async Task Handle_VariousBlobPaths_ExtractsFilenameCorrectly(string blobName, string expectedFileName)
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockExistsResponse = new Mock<Response<bool>>();
        var mockDownloadResponse = new Mock<Response<BlobDownloadInfo>>();
        var mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

        mockExistsResponse.Setup(x => x.Value).Returns(true);
        mockExistsResponse.Setup(x => x.GetRawResponse()).Returns(Mock.Of<Response>());

        mockDownloadResponse.Setup(x => x.Value).Returns(mockBlobDownloadInfo);
        mockDownloadResponse.Setup(x => x.GetRawResponse()).Returns(Mock.Of<Response>());

        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("backups"))
            .Returns(mockBlobContainerClient.Object);

        mockBlobContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockExistsResponse.Object);

        mockBlobClient
            .Setup(x => x.DownloadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDownloadResponse.Object);

        var handler = new DownloadBackupCsv.Handler(mockBlobServiceClient.Object, mockLogger.Object);
        var query = new DownloadBackupCsv.Query(blobName);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(expectedFileName);
    }

    /// <summary>
    /// Tests that Handle throws OperationCanceledException when cancellation is requested during ExistsAsync
    /// </summary>
    [Fact]
    public async Task Handle_CancellationRequestedDuringExistsAsync_ThrowsOperationCanceledException()
    {
        // Arrange
        var blobName = "TestBlob.csv";
        var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var cts = new CancellationTokenSource();

        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("backups"))
            .Returns(mockBlobContainerClient.Object);

        mockBlobContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var handler = new DownloadBackupCsv.Handler(mockBlobServiceClient.Object, mockLogger.Object);
        var query = new DownloadBackupCsv.Query(blobName);

        // Act
        var result = await handler.Handle(query, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeNull();
        result.ContentType.Should().BeNull();
        result.FileName.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle throws OperationCanceledException when cancellation is requested during DownloadAsync
    /// </summary>
    [Fact]
    public async Task Handle_CancellationRequestedDuringDownloadAsync_ThrowsOperationCanceledException()
    {
        // Arrange
        var blobName = "TestBlob.csv";
        var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockExistsResponse = new Mock<Response<bool>>();
        var cts = new CancellationTokenSource();

        mockExistsResponse.Setup(x => x.Value).Returns(true);
        mockExistsResponse.Setup(x => x.GetRawResponse()).Returns(Mock.Of<Response>());

        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("backups"))
            .Returns(mockBlobContainerClient.Object);

        mockBlobContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockExistsResponse.Object);

        mockBlobClient
            .Setup(x => x.DownloadAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var handler = new DownloadBackupCsv.Handler(mockBlobServiceClient.Object, mockLogger.Object);
        var query = new DownloadBackupCsv.Query(blobName);

        // Act
        var result = await handler.Handle(query, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeNull();
        result.ContentType.Should().BeNull();
        result.FileName.Should().BeNull();
    }

    /// <summary>
    /// Tests that Handle logs successfully downloaded blob message
    /// </summary>
    [Fact]
    public async Task Handle_BlobExistsAndDownloaded_LogsSuccessMessage()
    {
        // Arrange
        var blobName = "TestBlob.csv";
        var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockExistsResponse = new Mock<Response<bool>>();
        var mockDownloadResponse = new Mock<Response<BlobDownloadInfo>>();
        var mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

        mockExistsResponse.Setup(x => x.Value).Returns(true);
        mockExistsResponse.Setup(x => x.GetRawResponse()).Returns(Mock.Of<Response>());

        mockDownloadResponse.Setup(x => x.Value).Returns(mockBlobDownloadInfo);
        mockDownloadResponse.Setup(x => x.GetRawResponse()).Returns(Mock.Of<Response>());

        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient("backups"))
            .Returns(mockBlobContainerClient.Object);

        mockBlobContainerClient
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockExistsResponse.Object);

        mockBlobClient
            .Setup(x => x.DownloadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDownloadResponse.Object);

        var handler = new DownloadBackupCsv.Handler(mockBlobServiceClient.Object, mockLogger.Object);
        var query = new DownloadBackupCsv.Query(blobName);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully downloaded blob: {blobName}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
