using Budget.Api.Features.Admin.Roles;

namespace Budget.ApiTests.Features.Admin.Roles;


/// <summary>
/// Unit tests for CreateRole.Handler
/// </summary>
public partial class CreateRoleHandlerTests
{
    /// <summary>
    /// Creates in-memory database options for testing
    /// </summary>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<BudgetContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
          .Options;
    }

    /// <summary>
    /// Tests that Handle creates a role successfully with valid input and returns correct response
    /// </summary>
    [Fact]
    public async Task Handle_ValidInput_CreatesRoleAndReturnsResponse()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        var command = new CreateRole.Command("TestRole", "Test Description");

        // Act
        CreateRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestRole");
        result.Description.Should().Be("Test Description");
        result.Id.Should().BeGreaterThan(0);

        Role? savedRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "TestRole", cancellationToken: TestContext.Current.CancellationToken);

        savedRole.Should().NotBeNull();
        savedRole!.Name.Should().Be("TestRole");
        savedRole.Description.Should().Be("Test Description");
        savedRole.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests that Handle creates a role with empty description successfully
    /// </summary>
    [Fact]
    public async Task Handle_EmptyDescription_CreatesRoleSuccessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        var command = new CreateRole.Command("RoleWithEmptyDesc", "");

        // Act
        CreateRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("RoleWithEmptyDesc");
        result.Description.Should().BeEmpty();
        result.Id.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that Handle creates a role with special characters in name successfully
    /// </summary>
    [Fact]
    public async Task Handle_SpecialCharactersInName_CreatesRoleSuccessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        var command = new CreateRole.Command("Test-Role_123", "Description with special chars: !@#$%");

        // Act
        CreateRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test-Role_123");
        result.Description.Should().Be("Description with special chars: !@#$%");
        result.Id.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that Handle creates a role with whitespace-only description successfully
    /// </summary>
    [Fact]
    public async Task Handle_WhitespaceDescription_CreatesRoleSuccessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        var command = new CreateRole.Command("WhitespaceDescRole", "   ");

        // Act
        CreateRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("WhitespaceDescRole");
        result.Description.Should().Be("   ");
        result.Id.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that Handle creates a role with maximum allowed name length successfully
    /// </summary>
    [Fact]
    public async Task Handle_MaxLengthName_CreatesRoleSuccessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        string maxLengthName = new('A', 50);
        var command = new CreateRole.Command(maxLengthName, "Test Description");

        // Act
        CreateRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(maxLengthName);
        result.Id.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that Handle creates a role with maximum allowed description length successfully
    /// </summary>
    [Fact]
    public async Task Handle_MaxLengthDescription_CreatesRoleSuccessfully()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        string maxLengthDescription = new('B', 200);
        var command = new CreateRole.Command("MaxDescRole", maxLengthDescription);

        // Act
        CreateRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be(maxLengthDescription);
        result.Id.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that Handle throws OperationCanceledException when cancellation token is cancelled
    /// </summary>
    [Fact]
    public async Task Handle_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        var command = new CreateRole.Command("CancelledRole", "Description");
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        Func<Task> act = async () => await handler.Handle(command, cancellationTokenSource.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Tests that Handle sets CreatedAt timestamp correctly to current UTC time
    /// </summary>
    [Fact]
    public async Task Handle_ValidInput_SetsCreatedAtToUtcNow()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        var command = new CreateRole.Command("TimestampRole", "Test Description");
        DateTime beforeCreation = DateTime.UtcNow;

        // Act
        CreateRole.Response result = await handler.Handle(command, CancellationToken.None);
        DateTime afterCreation = DateTime.UtcNow;

        // Assert
        Role? savedRole = await context.Roles.FindAsync([result.Id], TestContext.Current.CancellationToken);
        savedRole.Should().NotBeNull();
        savedRole!.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        savedRole.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    /// <summary>
    /// Tests that Handle correctly generates unique IDs for multiple roles
    /// </summary>
    [Fact]
    public async Task Handle_MultipleRoles_GeneratesUniqueIds()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        var command1 = new CreateRole.Command("Role1", "Description1");
        var command2 = new CreateRole.Command("Role2", "Description2");
        var command3 = new CreateRole.Command("Role3", "Description3");

        // Act
        CreateRole.Response result1 = await handler.Handle(command1, CancellationToken.None);
        CreateRole.Response result2 = await handler.Handle(command2, CancellationToken.None);
        CreateRole.Response result3 = await handler.Handle(command3, CancellationToken.None);

        // Assert
        result1.Id.Should().BeGreaterThan(0);
        result2.Id.Should().BeGreaterThan(0);
        result3.Id.Should().BeGreaterThan(0);
        result1.Id.Should().NotBe(result2.Id);
        result2.Id.Should().NotBe(result3.Id);
        result1.Id.Should().NotBe(result3.Id);

        int roleCount = await context.Roles.CountAsync(TestContext.Current.CancellationToken);
        roleCount.Should().BeGreaterThanOrEqualTo(3);
    }

    /// <summary>
    /// Tests that Handle persists role to database correctly
    /// </summary>
    [Fact]
    public async Task Handle_ValidInput_PersistsRoleToDatabase()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        var command = new CreateRole.Command("PersistedRole", "Persisted Description");

        // Act
        CreateRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Role? roleFromDb = await context.Roles
          .Where(r => r.Id == result.Id)
          .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        roleFromDb.Should().NotBeNull();
        roleFromDb!.Name.Should().Be("PersistedRole");
        roleFromDb.Description.Should().Be("Persisted Description");
    }

    /// <summary>
    /// Tests that Handle returns response with all properties correctly populated
    /// </summary>
    [Fact]
    public async Task Handle_ValidInput_ReturnsCompleteResponse()
    {
        // Arrange
        await using var context = new BudgetContext(CreateInMemoryOptions(), null);
        var handler = new CreateRole.Handler(context);
        var command = new CreateRole.Command("CompleteRole", "Complete Description");

        // Act
        CreateRole.Response result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("CompleteRole");
        result.Description.Should().NotBeNull();
        result.Description.Should().Be("Complete Description");
    }
}
