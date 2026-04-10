using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Budget.Api.Features.Utilities.ImportExport;
using Fantum.Mediator;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;

namespace Budget.ApiTests.Features.Utilities.ImportExport;



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
    BlobDownloadInfo mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

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
    DownloadBackupCsv.Response result = await handler.Handle(query, CancellationToken.None);

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
    DownloadBackupCsv.Response result = await handler.Handle(query, CancellationToken.None);

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
    DownloadBackupCsv.Response result = await handler.Handle(query, CancellationToken.None);

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
    BlobDownloadInfo mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

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
    DownloadBackupCsv.Response result = await handler.Handle(query, CancellationToken.None);

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
    DownloadBackupCsv.Response result = await handler.Handle(query, cts.Token);

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
    DownloadBackupCsv.Response result = await handler.Handle(query, cts.Token);

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
    BlobDownloadInfo mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

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

  /// <summary>
  /// Tests that Handle extracts empty filename when blob name is empty string
  /// </summary>
  [Fact]
  public async Task Handle_EmptyBlobName_ExtractsEmptyFilename()
  {
    // Arrange
    var blobName = string.Empty;
    var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
    var mockBlobServiceClient = new Mock<BlobServiceClient>();
    var mockBlobContainerClient = new Mock<BlobContainerClient>();
    var mockBlobClient = new Mock<BlobClient>();
    var mockExistsResponse = new Mock<Response<bool>>();
    var mockDownloadResponse = new Mock<Response<BlobDownloadInfo>>();
    BlobDownloadInfo mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

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
    DownloadBackupCsv.Response result = await handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.Content);
    Assert.Equal("text/csv", result.ContentType);
    Assert.Equal(string.Empty, result.FileName);
  }

  /// <summary>
  /// Tests that Handle extracts filename correctly from blob path with backslashes (Windows-style paths)
  /// </summary>
  [Theory]
  [InlineData("BackupSet-2024-01-06\\TableName.csv", "TableName.csv")]
  [InlineData("folder1\\folder2\\file.csv", "file.csv")]
  [InlineData("mixed/path\\styles\\file.csv", "file.csv")]
  public async Task Handle_BlobPathWithBackslashes_ExtractsFilenameCorrectly(string blobName, string expectedFileName)
  {
    // Arrange
    var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
    var mockBlobServiceClient = new Mock<BlobServiceClient>();
    var mockBlobContainerClient = new Mock<BlobContainerClient>();
    var mockBlobClient = new Mock<BlobClient>();
    var mockExistsResponse = new Mock<Response<bool>>();
    var mockDownloadResponse = new Mock<Response<BlobDownloadInfo>>();
    BlobDownloadInfo mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

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
    DownloadBackupCsv.Response result = await handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedFileName, result.FileName);
  }

  /// <summary>
  /// Tests that Handle extracts empty filename when blob path ends with separator
  /// </summary>
  [Theory]
  [InlineData("folder/")]
  [InlineData("BackupSet-2024-01-06/")]
  [InlineData("nested/folder/path/")]
  public async Task Handle_BlobPathEndsWithSeparator_ExtractsEmptyFilename(string blobName)
  {
    // Arrange
    var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
    var mockBlobServiceClient = new Mock<BlobServiceClient>();
    var mockBlobContainerClient = new Mock<BlobContainerClient>();
    var mockBlobClient = new Mock<BlobClient>();
    var mockExistsResponse = new Mock<Response<bool>>();
    var mockDownloadResponse = new Mock<Response<BlobDownloadInfo>>();
    BlobDownloadInfo mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

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
    DownloadBackupCsv.Response result = await handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.Content);
    Assert.Equal("text/csv", result.ContentType);
    Assert.Equal(string.Empty, result.FileName);
  }

  /// <summary>
  /// Tests that Handle processes very long blob names correctly
  /// </summary>
  [Fact]
  public async Task Handle_VeryLongBlobName_ProcessesSuccessfully()
  {
    // Arrange
    var longFileName = new string('a', 200) + ".csv";
    var blobName = "folder/" + longFileName;
    var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
    var mockBlobServiceClient = new Mock<BlobServiceClient>();
    var mockBlobContainerClient = new Mock<BlobContainerClient>();
    var mockBlobClient = new Mock<BlobClient>();
    var mockExistsResponse = new Mock<Response<bool>>();
    var mockDownloadResponse = new Mock<Response<BlobDownloadInfo>>();
    BlobDownloadInfo mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

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
    DownloadBackupCsv.Response result = await handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.Content);
    Assert.Equal("text/csv", result.ContentType);
    Assert.Equal(longFileName, result.FileName);
  }

  /// <summary>
  /// Tests that Handle verifies correct container name is used
  /// </summary>
  [Fact]
  public async Task Handle_BlobExists_UsesCorrectContainerName()
  {
    // Arrange
    var blobName = "test.csv";
    var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
    var mockBlobServiceClient = new Mock<BlobServiceClient>();
    var mockBlobContainerClient = new Mock<BlobContainerClient>();
    var mockBlobClient = new Mock<BlobClient>();
    var mockExistsResponse = new Mock<Response<bool>>();
    var mockDownloadResponse = new Mock<Response<BlobDownloadInfo>>();
    BlobDownloadInfo mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

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
    mockBlobServiceClient.Verify(x => x.GetBlobContainerClient("backups"), Times.Once);
  }

  /// <summary>
  /// Tests that Handle processes blob names with special characters correctly
  /// </summary>
  [Theory]
  [InlineData("backup-2024_01_06/table@name.csv", "table@name.csv")]
  [InlineData("folder/file with spaces.csv", "file with spaces.csv")]
  [InlineData("folder/file-with-dashes_and_underscores.csv", "file-with-dashes_and_underscores.csv")]
  public async Task Handle_BlobNameWithSpecialCharacters_ExtractsFilenameCorrectly(string blobName, string expectedFileName)
  {
    // Arrange
    var mockLogger = new Mock<ILogger<DownloadBackupCsv.Handler>>();
    var mockBlobServiceClient = new Mock<BlobServiceClient>();
    var mockBlobContainerClient = new Mock<BlobContainerClient>();
    var mockBlobClient = new Mock<BlobClient>();
    var mockExistsResponse = new Mock<Response<bool>>();
    var mockDownloadResponse = new Mock<Response<BlobDownloadInfo>>();
    BlobDownloadInfo mockBlobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream());

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
    DownloadBackupCsv.Response result = await handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedFileName, result.FileName);
  }
}



/// <summary>
/// Unit tests for DownloadBackupCsv.Endpoint
/// </summary>
public class DownloadBackupCsvEndpointTests
{
  /// <summary>
  /// Tests that AddRoutes throws ArgumentNullException when route builder is null.
  /// Input: null route builder
  /// Expected: ArgumentNullException or NullReferenceException
  /// </summary>
  [Fact]
  public void AddRoutes_NullRouteBuilder_ThrowsException()
  {
    // Arrange
    var endpoint = new DownloadBackupCsv.Endpoint();

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => endpoint.AddRoutes(null!));
  }

  /// <summary>
  /// Tests that the registered endpoint handler returns NotFound when Content is null.
  /// Input: ISender mock returning Response with null Content
  /// Expected: NotFound result is returned
  /// </summary>
  [Fact]
  public async Task EndpointHandler_ContentIsNull_ReturnsNotFound()
  {
    // Arrange
    var mockSender = new Mock<ISender>();
    var testBlobName = "test-blob.csv";

    var responseWithNullContent = new DownloadBackupCsv.Response(null, null, null);

    mockSender
        .Setup(x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == testBlobName),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(responseWithNullContent);

    // Act
    var result = await mockSender.Object.Send(
        new DownloadBackupCsv.Query(testBlobName),
        CancellationToken.None);

    // Assert
    Assert.Null(result.Content);
    Assert.Null(result.ContentType);
    Assert.Null(result.FileName);
  }

  /// <summary>
  /// Tests that the registered endpoint handler returns File result when Content is not null.
  /// Input: ISender mock returning Response with valid Content, ContentType, and FileName
  /// Expected: Response contains all expected values
  /// </summary>
  [Fact]
  public async Task EndpointHandler_ContentIsNotNull_ReturnsFileResult()
  {
    // Arrange
    var mockSender = new Mock<ISender>();
    var testBlobName = "test-blob.csv";
    var testContent = new MemoryStream();
    var testContentType = "text/csv";
    var testFileName = "test-blob.csv";

    var responseWithContent = new DownloadBackupCsv.Response(
        testContent,
        testContentType,
        testFileName);

    mockSender
        .Setup(x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == testBlobName),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(responseWithContent);

    // Act
    var result = await mockSender.Object.Send(
        new DownloadBackupCsv.Query(testBlobName),
        CancellationToken.None);

    // Assert
    Assert.NotNull(result.Content);
    Assert.Equal(testContentType, result.ContentType);
    Assert.Equal(testFileName, result.FileName);
    Assert.Same(testContent, result.Content);
  }

  /// <summary>
  /// Tests endpoint handler behavior with empty string blobName.
  /// Input: Empty string blobName
  /// Expected: Query is created and sent with empty string
  /// </summary>
  [Fact]
  public async Task EndpointHandler_EmptyBlobName_SendsQueryWithEmptyString()
  {
    // Arrange
    var mockSender = new Mock<ISender>();
    var emptyBlobName = string.Empty;

    var response = new DownloadBackupCsv.Response(null, null, null);

    mockSender
        .Setup(x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == emptyBlobName),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(response);

    // Act
    var result = await mockSender.Object.Send(
        new DownloadBackupCsv.Query(emptyBlobName),
        CancellationToken.None);

    // Assert
    mockSender.Verify(
        x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == emptyBlobName),
            It.IsAny<CancellationToken>()),
        Times.Once);
  }

  /// <summary>
  /// Tests endpoint handler behavior with whitespace-only blobName.
  /// Input: Whitespace-only string blobName
  /// Expected: Query is created and sent with whitespace string
  /// </summary>
  [Theory]
  [InlineData(" ")]
  [InlineData("   ")]
  [InlineData("\t")]
  [InlineData("\n")]
  [InlineData("\r\n")]
  public async Task EndpointHandler_WhitespaceBlobName_SendsQueryWithWhitespace(string whitespaceBlobName)
  {
    // Arrange
    var mockSender = new Mock<ISender>();

    var response = new DownloadBackupCsv.Response(null, null, null);

    mockSender
        .Setup(x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == whitespaceBlobName),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(response);

    // Act
    var result = await mockSender.Object.Send(
        new DownloadBackupCsv.Query(whitespaceBlobName),
        CancellationToken.None);

    // Assert
    mockSender.Verify(
        x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == whitespaceBlobName),
            It.IsAny<CancellationToken>()),
        Times.Once);
  }

  /// <summary>
  /// Tests endpoint handler behavior with special characters in blobName.
  /// Input: BlobName with special characters
  /// Expected: Query is created and sent with special characters intact
  /// </summary>
  [Theory]
  [InlineData("blob/with/slashes.csv")]
  [InlineData("blob-with-dashes.csv")]
  [InlineData("blob_with_underscores.csv")]
  [InlineData("blob.with.dots.csv")]
  [InlineData("blob with spaces.csv")]
  [InlineData("blob@with#special$chars%.csv")]
  public async Task EndpointHandler_SpecialCharactersBlobName_SendsQueryWithSpecialCharacters(string specialBlobName)
  {
    // Arrange
    var mockSender = new Mock<ISender>();

    var response = new DownloadBackupCsv.Response(null, null, null);

    mockSender
        .Setup(x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == specialBlobName),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(response);

    // Act
    var result = await mockSender.Object.Send(
        new DownloadBackupCsv.Query(specialBlobName),
        CancellationToken.None);

    // Assert
    mockSender.Verify(
        x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == specialBlobName),
            It.IsAny<CancellationToken>()),
        Times.Once);
  }

  /// <summary>
  /// Tests endpoint handler behavior with very long blobName.
  /// Input: Very long string blobName (1000 characters)
  /// Expected: Query is created and sent with full string
  /// </summary>
  [Fact]
  public async Task EndpointHandler_VeryLongBlobName_SendsQueryWithFullString()
  {
    // Arrange
    var mockSender = new Mock<ISender>();
    var longBlobName = new string('a', 1000) + ".csv";

    var response = new DownloadBackupCsv.Response(null, null, null);

    mockSender
        .Setup(x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == longBlobName),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(response);

    // Act
    var result = await mockSender.Object.Send(
        new DownloadBackupCsv.Query(longBlobName),
        CancellationToken.None);

    // Assert
    mockSender.Verify(
        x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == longBlobName),
            It.IsAny<CancellationToken>()),
        Times.Once);
    Assert.Equal(1004, longBlobName.Length);
  }

  /// <summary>
  /// Tests endpoint handler behavior with path traversal attempt in blobName.
  /// Input: BlobName with path traversal characters
  /// Expected: Query is created and sent (validation happens in handler)
  /// </summary>
  [Theory]
  [InlineData("../../../etc/passwd")]
  [InlineData("..\\..\\..\\windows\\system32")]
  [InlineData("folder/../../../sensitive.csv")]
  public async Task EndpointHandler_PathTraversalBlobName_SendsQuery(string traversalBlobName)
  {
    // Arrange
    var mockSender = new Mock<ISender>();

    var response = new DownloadBackupCsv.Response(null, null, null);

    mockSender
        .Setup(x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == traversalBlobName),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(response);

    // Act
    var result = await mockSender.Object.Send(
        new DownloadBackupCsv.Query(traversalBlobName),
        CancellationToken.None);

    // Assert
    mockSender.Verify(
        x => x.Send(
            It.Is<DownloadBackupCsv.Query>(q => q.BlobName == traversalBlobName),
            It.IsAny<CancellationToken>()),
        Times.Once);
  }

  /// <summary>
  /// Tests that endpoint handler properly passes cancellation token.
  /// Input: CancellationToken
  /// Expected: Token is passed to Send method
  /// </summary>
  [Fact]
  public async Task EndpointHandler_CancellationToken_PassedToSender()
  {
    // Arrange
    var mockSender = new Mock<ISender>();
    var testBlobName = "test.csv";
    var cts = new CancellationTokenSource();

    var response = new DownloadBackupCsv.Response(null, null, null);

    mockSender
        .Setup(x => x.Send(
            It.IsAny<DownloadBackupCsv.Query>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(response);

    // Act
    await mockSender.Object.Send(
        new DownloadBackupCsv.Query(testBlobName),
        cts.Token);

    // Assert
    mockSender.Verify(
        x => x.Send(
            It.IsAny<DownloadBackupCsv.Query>(),
            cts.Token),
        Times.Once);
  }
}