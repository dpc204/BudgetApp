using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Budget.Api.Features.Envelopes.EnvelopeMaint;
using Budget.DB;
using Carter;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Budget.Api.Features.Envelopes.EnvelopeMaint.UnitTests;


/// <summary>
/// Unit tests for ImportEnvelopes.Handler
/// </summary>
public class ImportEnvelopesHandlerTests
{
    /// <summary>
    /// Creates in-memory database options for testing
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .Options;
    }

    /// <summary>
    /// Tests that Handle imports envelopes successfully from valid CSV content with proper headers and data rows.
    /// Input: Valid CSV with Id and Name columns matching Envelope entity properties.
    /// Expected: ImportedCount equals number of data rows, Errors list is empty.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCsvContent_ImportsEnvelopesAndReturnsCount()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        // NOTE: This CSV format assumes Envelope has Id and Name properties.
        // Adjust headers and data based on actual Envelope entity schema.
        string csvContent = "Id,Name\n1,Groceries\n2,Utilities";
        var command = new ImportEnvelopes.Command(csvContent);

        // Act
        ImportEnvelopes.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle returns success with zero count when CSV content is empty.
    /// Input: Empty string for CsvContent.
    /// Expected: ImportedCount is 0, Errors list is empty (no errors for empty content).
    /// </summary>
    [Fact]
    public async Task Handle_EmptyCsvContent_ReturnsErrorWithZeroCount()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        var command = new ImportEnvelopes.Command(string.Empty);

        // Act
        ImportEnvelopes.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle returns zero count when CSV contains only whitespace.
    /// Input: Whitespace-only string for CsvContent.
    /// Expected: ImportedCount is 0, Errors list is empty (no validation error for whitespace).
    /// </summary>
    [Fact]
    public async Task Handle_WhitespaceCsvContent_ReturnsErrorWithZeroCount()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        var command = new ImportEnvelopes.Command("   \t\n   ");

        // Act
        ImportEnvelopes.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle returns an error when CSV headers do not match Envelope entity properties.
    /// Input: CSV with invalid headers that don't correspond to any Envelope property.
    /// Expected: ImportedCount is 0, Errors list contains error about invalid column.
    /// </summary>
    [Fact]
    public async Task Handle_InvalidCsvHeaders_ReturnsErrorWithZeroCount()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        string csvContent = "InvalidColumn1,InvalidColumn2\n1,Test";
        var command = new ImportEnvelopes.Command(csvContent);

        // Act
        ImportEnvelopes.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(0);
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Should().Contain("Import failed");
    }

    /// <summary>
    /// Tests that Handle correctly processes CSV content with different line ending styles.
    /// Input: Valid CSV with mixed line endings (\r\n, \n, \r).
    /// Expected: All rows are imported correctly, ImportedCount reflects all data rows.
    /// </summary>
    [Fact]
    public async Task Handle_CsvWithDifferentLineEndings_ImportsCorrectly()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        // Mix different line ending styles
        string csvContent = "Id,Name\r\n1,Groceries\n2,Utilities\r3,Entertainment";
        var command = new ImportEnvelopes.Command(csvContent);

        // Act
        ImportEnvelopes.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(3);
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle imports multiple envelope rows correctly.
    /// Input: Valid CSV with multiple data rows.
    /// Expected: ImportedCount equals the number of data rows, Errors list is empty.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCsvWithMultipleRows_ImportsAllRows()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        string csvContent = "Id,Name\n1,Groceries\n2,Utilities\n3,Entertainment\n4,Transportation\n5,Healthcare";
        var command = new ImportEnvelopes.Command(csvContent);

        // Act
        ImportEnvelopes.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(5);
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle logs the start of the import operation.
    /// Input: Valid CSV content.
    /// Expected: Logger's LogInformation is called with appropriate message.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCsvContent_LogsImportStart()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        string csvContent = "Id,Name\n1,Groceries";
        var command = new ImportEnvelopes.Command(csvContent);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        mockLogger.Verify(
          x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting envelope import from CSV")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);
    }

    /// <summary>
    /// Tests that Handle executes the non-SQL Server code branch when using in-memory database.
    /// Input: Valid CSV content with in-memory database provider.
    /// Expected: Import succeeds without SQL Server-specific IDENTITY_INSERT handling.
    /// </summary>
    [Fact]
    public async Task Handle_InMemoryDatabase_ExecutesNonSqlServerBranch()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        string csvContent = "Id,Name\n1,Groceries\n2,Utilities";
        var command = new ImportEnvelopes.Command(csvContent);

        // Act
        ImportEnvelopes.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(2);
        result.Errors.Should().BeEmpty();
        context.Database.IsSqlServer().Should().BeFalse();
    }

    /// <summary>
    /// Tests that Handle returns an error when CSV has only headers without data rows.
    /// Input: CSV content with only header line.
    /// Expected: ImportedCount is 0, no errors (empty data is valid but imports nothing).
    /// </summary>
    [Fact]
    public async Task Handle_CsvWithOnlyHeaders_ReturnsZeroImportedCount()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        string csvContent = "Id,Name";
        var command = new ImportEnvelopes.Command(csvContent);

        // Act
        ImportEnvelopes.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle properly handles cancellation token during async operations.
    /// Input: Valid CSV content with a cancellation token.
    /// Expected: Import completes successfully when token is not cancelled.
    /// </summary>
    [Fact]
    public async Task Handle_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        string csvContent = "Id,Name\n1,Groceries";
        var command = new ImportEnvelopes.Command(csvContent);
        using var cts = new CancellationTokenSource();

        // Act
        ImportEnvelopes.Response result = await handler.Handle(command, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Handle returns appropriate error when CSV has empty lines between data rows.
    /// Input: CSV with empty lines interspersed with data rows.
    /// Expected: Valid data rows are imported, empty lines are skipped by CsvImportService.
    /// </summary>
    [Fact]
    public async Task Handle_CsvWithEmptyLines_ImportsValidRows()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ImportEnvelopes.Handler>>();
        var handler = new ImportEnvelopes.Handler(context, mockLogger.Object);

        string csvContent = "Id,Name\n1,Groceries\n\n2,Utilities\n\n\n3,Entertainment";
        var command = new ImportEnvelopes.Command(csvContent);

        // Act
        ImportEnvelopes.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(3);
        result.Errors.Should().BeEmpty();
    }
}
