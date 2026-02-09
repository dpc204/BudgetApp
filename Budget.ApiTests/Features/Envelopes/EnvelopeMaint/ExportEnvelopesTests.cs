using System;
using System.Threading;
using System.Threading.Tasks;

using Budget.Api.Features.Envelopes.EnvelopeMaint;
using Budget.DB;
using Carter;
using Fantum.Mediator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Budget.Api.Features.Envelopes.EnvelopeMaint.UnitTests;


/// <summary>
/// Unit tests for ExportEnvelopes.Handler
/// </summary>
public class ExportEnvelopesTests
{
    /// <summary>
    /// Creates in-memory database options for testing
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
          .Options;
    }

    /// <summary>
    /// Tests that Handle returns empty CSV when no envelopes exist in database
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsEmptyCsv()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var mockLogger = new Mock<ILogger<ExportEnvelopes.Handler>>();

        var handler = new ExportEnvelopes.Handler(context, mockLogger.Object);

        // Act
        string result = await handler.Handle(new ExportEnvelopes.Query(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        mockLogger.Verify(
          x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting envelope export to CSV")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);
        mockLogger.Verify(
          x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exported 0 envelopes to CSV")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);
    }

}