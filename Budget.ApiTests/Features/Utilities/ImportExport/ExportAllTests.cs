using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Budget.Api.Features.Utilities.ImportExport;
using Budget.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Budget.ApiTests.Features.Utilities.ImportExport;


/// <summary>
/// Tests for ExportAll.Handler.Handle method
/// </summary>
public class ExportAllHandlerTests
{
    /// <summary>
    /// Tests that Handle returns a successful response with the backup ID from the progress service
    /// when called with a valid request.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidRequest_ReturnsSuccessResponseWithBackupId()
    {
        // Arrange
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockProgressService = new Mock<IBackupProgressService>();
        var mockLogger = new Mock<ILogger<ExportAll.Handler>>();

        string expectedBackupId = "backup-12345";
        mockProgressService.Setup(x => x.StartBackup()).Returns(expectedBackupId);

        var handler = new ExportAll.Handler(
          mockServiceScopeFactory.Object,
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockProgressService.Object,
          mockLogger.Object);

        var command = new ExportAll.Command();
        var cancellationToken = CancellationToken.None;

        // Act
        ExportAll.Response response = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(expectedBackupId, response.BackupId);
        Assert.Equal("Backup started successfully", response.Message);
    }

    /// <summary>
    /// Tests that Handle calls StartBackup on the progress service exactly once
    /// to initiate backup tracking.
    /// </summary>
    [Fact]
    public async Task Handle_CallsStartBackup_ExactlyOnce()
    {
        // Arrange
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockProgressService = new Mock<IBackupProgressService>();
        var mockLogger = new Mock<ILogger<ExportAll.Handler>>();

        mockProgressService.Setup(x => x.StartBackup()).Returns("backup-123");

        var handler = new ExportAll.Handler(
          mockServiceScopeFactory.Object,
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockProgressService.Object,
          mockLogger.Object);

        var command = new ExportAll.Command();
        var cancellationToken = CancellationToken.None;

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        mockProgressService.Verify(x => x.StartBackup(), Times.Once);
    }

    /// <summary>
    /// Tests that Handle logs an informational message with the backup ID and partition key
    /// when starting the export process.
    /// </summary>
    [Fact]
    public async Task Handle_LogsInformationMessage_WithBackupIdAndPartitionKey()
    {
        // Arrange
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockProgressService = new Mock<IBackupProgressService>();
        var mockLogger = new Mock<ILogger<ExportAll.Handler>>();

        string expectedBackupId = "backup-abc";
        mockProgressService.Setup(x => x.StartBackup()).Returns(expectedBackupId);

        var handler = new ExportAll.Handler(
          mockServiceScopeFactory.Object,
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockProgressService.Object,
          mockLogger.Object);

        var command = new ExportAll.Command();
        var cancellationToken = CancellationToken.None;

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        mockLogger.Verify(
          x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting full database export") && v.ToString()!.Contains(expectedBackupId)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);
    }

    /// <summary>
    /// Tests that Handle returns immediately without waiting for the background backup task,
    /// completing in a very short time.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsImmediately_WithoutWaitingForBackgroundTask()
    {
        // Arrange
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockProgressService = new Mock<IBackupProgressService>();
        var mockLogger = new Mock<ILogger<ExportAll.Handler>>();

        mockProgressService.Setup(x => x.StartBackup()).Returns("backup-123");

        var handler = new ExportAll.Handler(
          mockServiceScopeFactory.Object,
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockProgressService.Object,
          mockLogger.Object);

        var command = new ExportAll.Command();
        var cancellationToken = CancellationToken.None;

        var startTime = DateTime.UtcNow;

        // Act
        ExportAll.Response response = await handler.Handle(command, cancellationToken);

        // Assert
        var duration = DateTime.UtcNow - startTime;
        Assert.True(duration.TotalMilliseconds < 1000, "Handle should return quickly without awaiting background task");
        Assert.NotNull(response);
    }

    /// <summary>
    /// Tests that Handle works correctly even when provided with an already-cancelled cancellation token,
    /// returning a successful response.
    /// </summary>
    [Fact]
    public async Task Handle_WithCancelledToken_StillReturnsSuccessResponse()
    {
        // Arrange
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockProgressService = new Mock<IBackupProgressService>();
        var mockLogger = new Mock<ILogger<ExportAll.Handler>>();

        string expectedBackupId = "backup-cancelled";
        mockProgressService.Setup(x => x.StartBackup()).Returns(expectedBackupId);

        var handler = new ExportAll.Handler(
          mockServiceScopeFactory.Object,
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockProgressService.Object,
          mockLogger.Object);

        var command = new ExportAll.Command();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancellationToken = cts.Token;

        // Act
        ExportAll.Response response = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(expectedBackupId, response.BackupId);
        Assert.Equal("Backup started successfully", response.Message);
    }

    /// <summary>
    /// Tests that Handle correctly uses an empty string backup ID returned by the progress service
    /// in the response object.
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyBackupId_ReturnsResponseWithEmptyBackupId()
    {
        // Arrange
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockProgressService = new Mock<IBackupProgressService>();
        var mockLogger = new Mock<ILogger<ExportAll.Handler>>();

        mockProgressService.Setup(x => x.StartBackup()).Returns(string.Empty);

        var handler = new ExportAll.Handler(
          mockServiceScopeFactory.Object,
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockProgressService.Object,
          mockLogger.Object);

        var command = new ExportAll.Command();
        var cancellationToken = CancellationToken.None;

        // Act
        ExportAll.Response response = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(string.Empty, response.BackupId);
        Assert.Equal("Backup started successfully", response.Message);
    }

    /// <summary>
    /// Tests that Handle correctly handles special characters in the backup ID returned by the progress service
    /// and includes them in the response.
    /// </summary>
    [Theory]
    [InlineData("backup-with-special-chars-!@#$%")]
    [InlineData("backup\twith\ttabs")]
    [InlineData("backup\nwith\nnewlines")]
    [InlineData("backup-with-unicode-中文")]
    public async Task Handle_WithSpecialCharactersInBackupId_ReturnsResponseWithCorrectBackupId(string backupId)
    {
        // Arrange
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockProgressService = new Mock<IBackupProgressService>();
        var mockLogger = new Mock<ILogger<ExportAll.Handler>>();

        mockProgressService.Setup(x => x.StartBackup()).Returns(backupId);

        var handler = new ExportAll.Handler(
          mockServiceScopeFactory.Object,
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockProgressService.Object,
          mockLogger.Object);

        var command = new ExportAll.Command();
        var cancellationToken = CancellationToken.None;

        // Act
        ExportAll.Response response = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(backupId, response.BackupId);
        Assert.Equal("Backup started successfully", response.Message);
    }

    /// <summary>
    /// Tests that Handle correctly handles very long backup IDs returned by the progress service
    /// and includes them in the response without truncation.
    /// </summary>
    [Fact]
    public async Task Handle_WithVeryLongBackupId_ReturnsResponseWithFullBackupId()
    {
        // Arrange
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockProgressService = new Mock<IBackupProgressService>();
        var mockLogger = new Mock<ILogger<ExportAll.Handler>>();

        string veryLongBackupId = new('a', 10000);
        mockProgressService.Setup(x => x.StartBackup()).Returns(veryLongBackupId);

        var handler = new ExportAll.Handler(
          mockServiceScopeFactory.Object,
          mockBlobServiceClient.Object,
          mockTableServiceClient.Object,
          mockProgressService.Object,
          mockLogger.Object);

        var command = new ExportAll.Command();
        var cancellationToken = CancellationToken.None;

        // Act
        ExportAll.Response response = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(veryLongBackupId, response.BackupId);
        Assert.Equal(10000, response.BackupId.Length);
    }
}