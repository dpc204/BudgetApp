using Budget.Api.Features.Utilities.ImportExport;
using Budget.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Budget.ApiTests.Features.Utilities.ImportExport;


/// <summary>
/// Unit tests for the GetBackupStatus.Endpoint class
/// </summary>
public partial class EndpointTests
{
    /// <summary>
    /// Tests that AddRoutes successfully registers the route without throwing exceptions.
    /// Input: Valid IEndpointRouteBuilder mock
    /// Expected: Method completes without throwing
    /// Note: Full endpoint behavior testing (route path, HTTP method, authorization, handler logic)
    /// requires integration testing with WebApplicationFactory. This unit test verifies basic
    /// invocation without exceptions.
    /// </summary>
    [Fact]
    public void AddRoutes_ValidApp_RegistersRouteWithoutException()
    {
        // Arrange
        var mockRouteBuilder = new Mock<IEndpointRouteBuilder>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var dataSources = new List<EndpointDataSource>();
        
        mockRouteBuilder.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockRouteBuilder.Setup(x => x.DataSources).Returns(dataSources);
        mockRouteBuilder.Setup(x => x.CreateApplicationBuilder()).Returns(new ApplicationBuilder(mockServiceProvider.Object));

        var endpoint = new GetBackupStatus.Endpoint();

        // Act
        var exception = Record.Exception(() => endpoint.AddRoutes(mockRouteBuilder.Object));

        // Assert
        Assert.Null(exception);
    }
}


/// <summary>
/// Tests for GetBackupStatus.Handler
/// </summary>
public class GetBackupStatusHandlerTests
{
    /// <summary>
    /// Tests that Handle returns null when the backup status is not found.
    /// </summary>
    [Fact]
    public async Task Handle_WithNullBackupStatus_ReturnsNull()
    {
        // Arrange
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus(It.IsAny<string>())).Returns((BackupStatus?)null);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query("test-backup-id");

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that Handle correctly maps all properties from BackupStatus to Response.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidBackupStatus_ReturnsResponseWithAllPropertiesMapped()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow.AddMinutes(5);
        var backupStatus = new BackupStatus(
          BackupId: "backup-123",
          StartTime: startTime,
          EndTime: endTime,
          TotalTables: 10,
          CompletedTables: 8,
          FailedTables: 2,
          CurrentTable: "Users",
          ErrorMessage: "Some error",
          IsComplete: true
        );
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus("backup-123")).Returns(backupStatus);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query("backup-123");

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("backup-123", result.BackupId);
        Assert.Equal(startTime, result.StartTime);
        Assert.Equal(endTime, result.EndTime);
        Assert.Equal(10, result.TotalTables);
        Assert.Equal(8, result.CompletedTables);
        Assert.Equal(2, result.FailedTables);
        Assert.Equal("Users", result.CurrentTable);
        Assert.Equal("Some error", result.ErrorMessage);
        Assert.True(result.IsComplete);
    }

    /// <summary>
    /// Tests that Handle correctly maps BackupStatus with null optional fields.
    /// </summary>
    [Fact]
    public async Task Handle_WithNullOptionalFields_ReturnsResponseWithNullOptionalFields()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var backupStatus = new BackupStatus(
          BackupId: "backup-456",
          StartTime: startTime,
          EndTime: null,
          TotalTables: 5,
          CompletedTables: 3,
          FailedTables: 0,
          CurrentTable: null,
          ErrorMessage: null,
          IsComplete: false
        );
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus("backup-456")).Returns(backupStatus);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query("backup-456");

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("backup-456", result.BackupId);
        Assert.Equal(startTime, result.StartTime);
        Assert.Null(result.EndTime);
        Assert.Equal(5, result.TotalTables);
        Assert.Equal(3, result.CompletedTables);
        Assert.Equal(0, result.FailedTables);
        Assert.Null(result.CurrentTable);
        Assert.Null(result.ErrorMessage);
        Assert.False(result.IsComplete);
    }

    /// <summary>
    /// Tests Handle with various BackupId edge cases including null, empty, and whitespace.
    /// </summary>
    /// <param name="backupId">The backup ID to test.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Handle_WithEdgeCaseBackupIds_CallsGetStatusWithProvidedId(string? backupId)
    {
        // Arrange
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus(It.IsAny<string>())).Returns((BackupStatus?)null);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query(backupId!);

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        mockProgressService.Verify(x => x.GetStatus(backupId!), Times.Once);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests Handle with extreme numeric values for table counts.
    /// </summary>
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(-1, -1, -1)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue)]
    [InlineData(int.MinValue, int.MinValue, int.MinValue)]
    [InlineData(1000000, 500000, 100000)]
    public async Task Handle_WithExtremeNumericValues_MapsValuesCorrectly(int totalTables, int completedTables, int failedTables)
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var backupStatus = new BackupStatus(
          BackupId: "numeric-test",
          StartTime: startTime,
          EndTime: null,
          TotalTables: totalTables,
          CompletedTables: completedTables,
          FailedTables: failedTables,
          CurrentTable: null,
          ErrorMessage: null,
          IsComplete: false
        );
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus("numeric-test")).Returns(backupStatus);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query("numeric-test");

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(totalTables, result.TotalTables);
        Assert.Equal(completedTables, result.CompletedTables);
        Assert.Equal(failedTables, result.FailedTables);
    }

    /// <summary>
    /// Tests Handle with extreme DateTime values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    public async Task Handle_WithExtremeDateTimeValues_MapsValuesCorrectly(int dateTimeTicks)
    {
        // Arrange
        var startTime = dateTimeTicks switch
        {
            0 => DateTime.MinValue,
            1 => DateTime.MaxValue,
            _ => DateTime.UtcNow
        };
        var endTime = dateTimeTicks == 1 ? (DateTime?)DateTime.MaxValue : null;
        var backupStatus = new BackupStatus(
          BackupId: "datetime-test",
          StartTime: startTime,
          EndTime: endTime,
          TotalTables: 1,
          CompletedTables: 1,
          FailedTables: 0,
          CurrentTable: null,
          ErrorMessage: null,
          IsComplete: true
        );
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus("datetime-test")).Returns(backupStatus);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query("datetime-test");

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startTime, result.StartTime);
        Assert.Equal(endTime, result.EndTime);
    }

    /// <summary>
    /// Tests Handle with very long strings for BackupId, CurrentTable, and ErrorMessage.
    /// </summary>
    [Fact]
    public async Task Handle_WithVeryLongStrings_MapsValuesCorrectly()
    {
        // Arrange
        var longBackupId = new string('a', 10000);
        var longCurrentTable = new string('b', 10000);
        var longErrorMessage = new string('c', 10000);
        var startTime = DateTime.UtcNow;
        var backupStatus = new BackupStatus(
          BackupId: longBackupId,
          StartTime: startTime,
          EndTime: null,
          TotalTables: 1,
          CompletedTables: 0,
          FailedTables: 0,
          CurrentTable: longCurrentTable,
          ErrorMessage: longErrorMessage,
          IsComplete: false
        );
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus(longBackupId)).Returns(backupStatus);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query(longBackupId);

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longBackupId, result.BackupId);
        Assert.Equal(longCurrentTable, result.CurrentTable);
        Assert.Equal(longErrorMessage, result.ErrorMessage);
    }

    /// <summary>
    /// Tests Handle with strings containing special characters.
    /// </summary>
    [Fact]
    public async Task Handle_WithSpecialCharactersInStrings_MapsValuesCorrectly()
    {
        // Arrange
        var specialBackupId = "backup-!@#$%^&*()_+-={}[]|\\:;\"'<>,.?/~`";
        var specialCurrentTable = "Table\nWith\tSpecial\rCharacters";
        var specialErrorMessage = "Error: <script>alert('xss')</script>";
        var startTime = DateTime.UtcNow;
        var backupStatus = new BackupStatus(
          BackupId: specialBackupId,
          StartTime: startTime,
          EndTime: null,
          TotalTables: 1,
          CompletedTables: 0,
          FailedTables: 0,
          CurrentTable: specialCurrentTable,
          ErrorMessage: specialErrorMessage,
          IsComplete: false
        );
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus(specialBackupId)).Returns(backupStatus);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query(specialBackupId);

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(specialBackupId, result.BackupId);
        Assert.Equal(specialCurrentTable, result.CurrentTable);
        Assert.Equal(specialErrorMessage, result.ErrorMessage);
    }

    /// <summary>
    /// Tests Handle with empty strings for optional string fields.
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyStringsForOptionalFields_MapsValuesCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var backupStatus = new BackupStatus(
          BackupId: "empty-test",
          StartTime: startTime,
          EndTime: null,
          TotalTables: 1,
          CompletedTables: 0,
          FailedTables: 0,
          CurrentTable: "",
          ErrorMessage: "",
          IsComplete: false
        );
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus("empty-test")).Returns(backupStatus);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query("empty-test");

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("", result.CurrentTable);
        Assert.Equal("", result.ErrorMessage);
    }

    /// <summary>
    /// Tests Handle with EndTime before StartTime (logically invalid but should still map).
    /// </summary>
    [Fact]
    public async Task Handle_WithEndTimeBeforeStartTime_MapsValuesCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(-1);
        var backupStatus = new BackupStatus(
          BackupId: "time-test",
          StartTime: startTime,
          EndTime: endTime,
          TotalTables: 1,
          CompletedTables: 1,
          FailedTables: 0,
          CurrentTable: null,
          ErrorMessage: null,
          IsComplete: true
        );
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus("time-test")).Returns(backupStatus);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query("time-test");

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startTime, result.StartTime);
        Assert.Equal(endTime, result.EndTime);
        Assert.True(result.EndTime < result.StartTime);
    }

    /// <summary>
    /// Tests Handle with IsComplete set to true.
    /// </summary>
    [Fact]
    public async Task Handle_WithCompletedBackup_ReturnsResponseWithIsCompleteTrue()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(10);
        var backupStatus = new BackupStatus(
          BackupId: "completed-backup",
          StartTime: startTime,
          EndTime: endTime,
          TotalTables: 10,
          CompletedTables: 10,
          FailedTables: 0,
          CurrentTable: null,
          ErrorMessage: null,
          IsComplete: true
        );
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus("completed-backup")).Returns(backupStatus);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query("completed-backup");

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsComplete);
        Assert.NotNull(result.EndTime);
    }

    /// <summary>
    /// Tests Handle with IsComplete set to false.
    /// </summary>
    [Fact]
    public async Task Handle_WithInProgressBackup_ReturnsResponseWithIsCompleteFalse()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var backupStatus = new BackupStatus(
          BackupId: "inprogress-backup",
          StartTime: startTime,
          EndTime: null,
          TotalTables: 10,
          CompletedTables: 5,
          FailedTables: 0,
          CurrentTable: "Transactions",
          ErrorMessage: null,
          IsComplete: false
        );
        var mockProgressService = new Mock<IBackupProgressService>();
        mockProgressService.Setup(x => x.GetStatus("inprogress-backup")).Returns(backupStatus);
        var handler = new GetBackupStatus.Handler(mockProgressService.Object);
        var query = new GetBackupStatus.Query("inprogress-backup");

        // Act
        GetBackupStatus.Response? result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsComplete);
        Assert.Null(result.EndTime);
        Assert.Equal("Transactions", result.CurrentTable);
    }
}