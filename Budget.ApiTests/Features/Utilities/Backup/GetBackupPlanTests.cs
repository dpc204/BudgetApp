using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Budget.Api.Features.Utilities.Backup;
using Budget.DB;
using Carter;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Budget.Api.Features.Utilities.Backup.UnitTests;


/// <summary>
/// Unit tests for the GetBackupPlan.Handler class.
/// </summary>
public class HandlerTests
{
    /// <summary>
    /// Creates in-memory database options for testing.
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
          .Options;
    }

}
