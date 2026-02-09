namespace Budget.ApiTests.Features.Utilities.Backup;


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
