using Azure;
using Azure.Data.Tables;
using Budget.Api.Features.Utilities.ImportExport;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi.Generated;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Budget.ApiTests.Features.Utilities.ImportExport;
/// <summary>
/// Tests for GetBackupSets.Handler
/// </summary>
public class GetBackupSetsHandlerTests
{
    /// <summary>
    /// Tests that Handle returns an empty list when the table contains no entities.
    /// </summary>
    [Fact]
    public async Task Handle_EmptyTable_ReturnsEmptyList()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var emptyEntities = new List<TableEntity>();
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(emptyEntities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle returns a single backup set with correct values when table contains one entity.
    /// </summary>
    [Fact]
    public async Task Handle_SingleBackupSetWithOneEntity_ReturnsCorrectDto()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var backupDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entity = new TableEntity("backup-20240115", "table1")
        {
            ["SizeBytes"] = 1024,
            ["ExportedAt"] = backupDate
        };
        var entities = new List<TableEntity>
        {
            entity
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(1);
        result.BackupSets[0].PartitionKey.Should().Be("backup-20240115");
        result.BackupSets[0].BackupDate.Should().Be(backupDate);
        result.BackupSets[0].TableCount.Should().Be(1);
        result.BackupSets[0].TotalSizeBytes.Should().Be(1024);
    }

    /// <summary>
    /// Tests that Handle correctly groups multiple entities with the same PartitionKey.
    /// </summary>
    [Fact]
    public async Task Handle_MultipleEntitiesInSamePartition_GroupsCorrectly()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var backupDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entity1 = new TableEntity("backup-20240115", "table1")
        {
            ["SizeBytes"] = 1024,
            ["ExportedAt"] = backupDate
        };
        var entity2 = new TableEntity("backup-20240115", "table2")
        {
            ["SizeBytes"] = 2048,
            ["ExportedAt"] = backupDate
        };
        var entity3 = new TableEntity("backup-20240115", "table3")
        {
            ["SizeBytes"] = 512,
            ["ExportedAt"] = backupDate
        };
        var entities = new List<TableEntity>
        {
            entity1,
            entity2,
            entity3
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(1);
        result.BackupSets[0].PartitionKey.Should().Be("backup-20240115");
        result.BackupSets[0].TableCount.Should().Be(3);
        result.BackupSets[0].TotalSizeBytes.Should().Be(3584); // 1024 + 2048 + 512
    }

    /// <summary>
    /// Tests that Handle returns multiple backup sets and sorts them by date descending.
    /// </summary>
    [Fact]
    public async Task Handle_MultipleBackupSets_SortsByDateDescending()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var oldDate = new DateTime(2024, 1, 10, 10, 0, 0, DateTimeKind.Utc);
        var middleDate = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var newDate = new DateTime(2024, 1, 20, 10, 0, 0, DateTimeKind.Utc);
        var entity1 = new TableEntity("backup-20240110", "table1")
        {
            ["SizeBytes"] = 1000,
            ["ExportedAt"] = oldDate
        };
        var entity2 = new TableEntity("backup-20240115", "table1")
        {
            ["SizeBytes"] = 2000,
            ["ExportedAt"] = middleDate
        };
        var entity3 = new TableEntity("backup-20240120", "table1")
        {
            ["SizeBytes"] = 3000,
            ["ExportedAt"] = newDate
        };
        var entities = new List<TableEntity>
        {
            entity1,
            entity2,
            entity3
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(3);
        result.BackupSets[0].BackupDate.Should().Be(newDate);
        result.BackupSets[1].BackupDate.Should().Be(middleDate);
        result.BackupSets[2].BackupDate.Should().Be(oldDate);
    }

    /// <summary>
    /// Tests that Handle defaults SizeBytes to 0 when the property is null.
    /// </summary>
    [Fact]
    public async Task Handle_EntityWithNullSizeBytes_DefaultsToZero()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var backupDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entity = new TableEntity("backup-20240115", "table1")
        {
            ["ExportedAt"] = backupDate
        // SizeBytes intentionally not set
        };
        var entities = new List<TableEntity>
        {
            entity
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(1);
        result.BackupSets[0].TotalSizeBytes.Should().Be(0);
    }

    /// <summary>
    /// Tests that Handle defaults ExportedAt to DateTime.MinValue when the property is null.
    /// </summary>
    [Fact]
    public async Task Handle_EntityWithNullExportedAt_DefaultsToMinValue()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var entity = new TableEntity("backup-20240115", "table1")
        {
            ["SizeBytes"] = 1024
        // ExportedAt intentionally not set
        };
        var entities = new List<TableEntity>
        {
            entity
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(1);
        result.BackupSets[0].BackupDate.Should().Be(DateTime.MinValue);
    }

    /// <summary>
    /// Tests that Handle returns an empty response when CreateIfNotExistsAsync throws an exception.
    /// </summary>
    [Fact]
    public async Task Handle_CreateTableFails_ReturnsEmptyResponseAndLogsWarning()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Azure storage not available"));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().BeEmpty();
        mockLogger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Azure Table Storage not available")), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    /// <summary>
    /// Tests that Handle rethrows exceptions when QueryAsync fails.
    /// </summary>
    [Fact]
    public async Task Handle_QueryThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var exception = new InvalidOperationException("Query failed");
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new ThrowingAsyncPageable<TableEntity>(exception));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);
        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Query failed");
        mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Exception!!")), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    /// <summary>
    /// Tests that Handle passes the cancellation token to CreateIfNotExistsAsync.
    /// </summary>
    [Fact]
    public async Task Handle_CancellationToken_PassedToCreateIfNotExists()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(cancellationToken)).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var emptyEntities = new List<TableEntity>();
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), cancellationToken)).Returns(new TestAsyncPageable<TableEntity>(emptyEntities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        await handler.Handle(query, cancellationToken);
        // Assert
        mockTableClient.Verify(x => x.CreateIfNotExistsAsync(cancellationToken), Times.Once);
    }

    /// <summary>
    /// Tests that Handle correctly accumulates sizes across multiple entities with different sizes.
    /// </summary>
    [Fact]
    public async Task Handle_MultipleEntitiesWithDifferentSizes_AccumulatesCorrectly()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var backupDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entity1 = new TableEntity("backup-20240115", "table1")
        {
            ["SizeBytes"] = 0,
            ["ExportedAt"] = backupDate
        };
        var entity2 = new TableEntity("backup-20240115", "table2")
        {
            ["SizeBytes"] = int.MaxValue,
            ["ExportedAt"] = backupDate
        };
        var entities = new List<TableEntity>
        {
            entity1,
            entity2
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(1);
        result.BackupSets[0].TotalSizeBytes.Should().Be((long)int.MaxValue);
    }

    /// <summary>
    /// Tests that Handle correctly handles entities with boundary date values.
    /// </summary>
    [Fact]
    public async Task Handle_EntitiesWithBoundaryDates_HandlesCorrectly()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var entity1 = new TableEntity("backup-min", "table1")
        {
            ["SizeBytes"] = 100,
            ["ExportedAt"] = DateTime.MinValue
        };
        var entity2 = new TableEntity("backup-max", "table1")
        {
            ["SizeBytes"] = 200,
            ["ExportedAt"] = DateTime.MaxValue
        };
        var entities = new List<TableEntity>
        {
            entity1,
            entity2
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(2);
        result.BackupSets[0].BackupDate.Should().Be(DateTime.MaxValue);
        result.BackupSets[1].BackupDate.Should().Be(DateTime.MinValue);
    }

    /// <summary>
    /// Helper class to provide test data for AsyncPageable enumeration.
    /// </summary>
    private class TestAsyncPageable<T>(IEnumerable<T> items) : AsyncPageable<T> where T : notnull
    {
        private readonly IEnumerable<T> _items = items;
        public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            await Task.CompletedTask;
            yield return Page<T>.FromValues([.._items], null, Mock.Of<Response>());
        }
    }

    /// <summary>
    /// Helper class to simulate an AsyncPageable that throws an exception during enumeration.
    /// </summary>
    private class ThrowingAsyncPageable<T>(Exception exception) : AsyncPageable<T> where T : notnull
    {
        private readonly Exception _exception = exception;
        public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            await Task.CompletedTask;
            throw _exception;
#pragma warning disable CS0162 // Unreachable code detected

            yield break;
#pragma warning restore CS0162 // Unreachable code detected

        }
    }

    /// <summary>
    /// Tests that Handle correctly accumulates negative SizeBytes values.
    /// Input: Entities with negative SizeBytes values.
    /// Expected: TotalSizeBytes correctly accumulates negative values.
    /// </summary>
    [Fact]
    public async Task Handle_EntityWithNegativeSizeBytes_AccumulatesCorrectly()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var backupDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entity1 = new TableEntity("backup-20240115", "table1")
        {
            ["SizeBytes"] = -500,
            ["ExportedAt"] = backupDate
        };
        var entity2 = new TableEntity("backup-20240115", "table2")
        {
            ["SizeBytes"] = 1000,
            ["ExportedAt"] = backupDate
        };
        var entities = new List<TableEntity>
        {
            entity1,
            entity2
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(1);
        result.BackupSets[0].TotalSizeBytes.Should().Be(500); // -500 + 1000 = 500
        result.BackupSets[0].TableCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that Handle correctly handles int.MinValue for SizeBytes.
    /// Input: Entity with SizeBytes set to int.MinValue.
    /// Expected: TotalSizeBytes correctly stores int.MinValue as long.
    /// </summary>
    [Fact]
    public async Task Handle_EntityWithMinValueSizeBytes_HandlesCorrectly()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var backupDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entity = new TableEntity("backup-20240115", "table1")
        {
            ["SizeBytes"] = int.MinValue,
            ["ExportedAt"] = backupDate
        };
        var entities = new List<TableEntity>
        {
            entity
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(1);
        result.BackupSets[0].TotalSizeBytes.Should().Be((long)int.MinValue);
        result.BackupSets[0].TableCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that Handle correctly accumulates when some entities have null SizeBytes and others don't.
    /// Input: Multiple entities in same partition with mixed null and non-null SizeBytes.
    /// Expected: Null values default to 0 and accumulation works correctly.
    /// </summary>
    [Fact]
    public async Task Handle_MixedNullAndNonNullSizesInSamePartition_AccumulatesCorrectly()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var backupDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entity1 = new TableEntity("backup-20240115", "table1")
        {
            ["SizeBytes"] = 1024,
            ["ExportedAt"] = backupDate
        };
        var entity2 = new TableEntity("backup-20240115", "table2")
        {
            ["ExportedAt"] = backupDate
        // SizeBytes intentionally not set (null)
        };
        var entity3 = new TableEntity("backup-20240115", "table3")
        {
            ["SizeBytes"] = 2048,
            ["ExportedAt"] = backupDate
        };
        var entities = new List<TableEntity>
        {
            entity1,
            entity2,
            entity3
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(1);
        result.BackupSets[0].TotalSizeBytes.Should().Be(3072); // 1024 + 0 + 2048
        result.BackupSets[0].TableCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that Handle correctly groups entities with empty string PartitionKey.
    /// Input: Multiple entities with empty string as PartitionKey.
    /// Expected: Entities are grouped correctly under empty string key.
    /// </summary>
    [Fact]
    public async Task Handle_EmptyStringPartitionKey_GroupsCorrectly()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var backupDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entity1 = new TableEntity("", "table1")
        {
            ["SizeBytes"] = 500,
            ["ExportedAt"] = backupDate
        };
        var entity2 = new TableEntity("", "table2")
        {
            ["SizeBytes"] = 700,
            ["ExportedAt"] = backupDate
        };
        var entities = new List<TableEntity>
        {
            entity1,
            entity2
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(1);
        result.BackupSets[0].PartitionKey.Should().Be("");
        result.BackupSets[0].TableCount.Should().Be(2);
        result.BackupSets[0].TotalSizeBytes.Should().Be(1200);
    }

    /// <summary>
    /// Tests that Handle maintains stable order when multiple partitions have identical backup dates.
    /// Input: Multiple backup sets with the same BackupDate.
    /// Expected: Results are sorted by date descending and order is stable for same dates.
    /// </summary>
    [Fact]
    public async Task Handle_PartitionKeysWithSameDate_MaintainsStableOrder()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var sameDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entity1 = new TableEntity("backup-A", "table1")
        {
            ["SizeBytes"] = 100,
            ["ExportedAt"] = sameDate
        };
        var entity2 = new TableEntity("backup-B", "table1")
        {
            ["SizeBytes"] = 200,
            ["ExportedAt"] = sameDate
        };
        var entity3 = new TableEntity("backup-C", "table1")
        {
            ["SizeBytes"] = 300,
            ["ExportedAt"] = sameDate
        };
        var entities = new List<TableEntity>
        {
            entity1,
            entity2,
            entity3
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(3);
        result.BackupSets.Should().OnlyContain(x => x.BackupDate == sameDate);
        result.BackupSets.Select(x => x.PartitionKey).Should().Contain(new[] { "backup-A", "backup-B", "backup-C" });
    }

    /// <summary>
    /// Tests that Handle logs informational messages when successfully retrieving backup sets.
    /// Input: Valid query with entities.
    /// Expected: Logs "Retrieving backup sets" and "Found {Count} backup sets" messages.
    /// </summary>
    [Fact]
    public async Task Handle_SuccessfulRetrieval_LogsInformationMessages()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var backupDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entity = new TableEntity("backup-20240115", "table1")
        {
            ["SizeBytes"] = 1024,
            ["ExportedAt"] = backupDate
        };
        var entities = new List<TableEntity>
        {
            entity
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Retrieving backup sets from Azure Table Storage")), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Found 1 backup sets")), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    /// <summary>
    /// Tests that Handle correctly handles partition keys with special Unicode characters.
    /// Input: Entities with partition keys containing Unicode and special characters.
    /// Expected: Characters are preserved and entities are grouped correctly.
    /// </summary>
    [Fact]
    public async Task Handle_PartitionKeyWithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var mockTableServiceClient = new Mock<TableServiceClient>();
        var mockTableClient = new Mock<TableClient>();
        var mockLogger = new Mock<ILogger<GetBackupSets.Handler>>();
        mockTableServiceClient.Setup(x => x.GetTableClient("TableBackups")).Returns(mockTableClient.Object);
        mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Response<Azure.Data.Tables.Models.TableItem>>());
        var backupDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var unicodeKey = "backup-测试-🎯-αβγ";
        var entity1 = new TableEntity(unicodeKey, "table1")
        {
            ["SizeBytes"] = 500,
            ["ExportedAt"] = backupDate
        };
        var entity2 = new TableEntity(unicodeKey, "table2")
        {
            ["SizeBytes"] = 300,
            ["ExportedAt"] = backupDate
        };
        var entities = new List<TableEntity>
        {
            entity1,
            entity2
        };
        mockTableClient.Setup(x => x.QueryAsync<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(new TestAsyncPageable<TableEntity>(entities));
        var handler = new GetBackupSets.Handler(mockTableServiceClient.Object, mockLogger.Object);
        var query = new GetBackupSets.Query();
        // Act
        GetBackupSets.Response result = await handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.BackupSets.Should().HaveCount(1);
        result.BackupSets[0].PartitionKey.Should().Be(unicodeKey);
        result.BackupSets[0].TableCount.Should().Be(2);
        result.BackupSets[0].TotalSizeBytes.Should().Be(800);
    }
}

/// <summary>
/// Tests for GetBackupSets.Endpoint
/// </summary>
public class GetBackupSetsEndpointTests
{
}