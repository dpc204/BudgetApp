namespace Budget.ApiTests;

/// <summary>
/// Custom WebApplicationFactory for testing with in-memory database
/// </summary>
public class BudgetApiTestFactory : WebApplicationFactory<Budget.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
