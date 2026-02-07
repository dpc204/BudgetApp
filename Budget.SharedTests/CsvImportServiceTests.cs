using Budget.Shared.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Budget.SharedTests;

/// <summary>
/// Unit tests for the CsvImportService.
/// </summary>
public class CsvImportServiceTests : IDisposable
{
    private readonly string _tempDir;

    public CsvImportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"CsvImportTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    private string CreateTempFile(string content)
    {
        var filePath = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.csv");
        File.WriteAllText(filePath, content);
        return filePath;
    }

    #region Test Entities

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public decimal Balance { get; set; }
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class TestEntityWithNullables
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? LastDate { get; set; }
        public int? OptionalValue { get; set; }
    }

    public enum TestStatus
    {
        Active,
        Inactive,
        Pending
    }

    public class TestEntityWithEnum
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TestStatus Status { get; set; }
    }

    public class TestContext(DbContextOptions<CsvImportServiceTests.TestContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        public DbSet<TestEntityWithNullables> TestEntitiesWithNullables => Set<TestEntityWithNullables>();
        public DbSet<TestEntityWithEnum> TestEntitiesWithEnum => Set<TestEntityWithEnum>();
  }

  #endregion

  private static TestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestContext(options);
    }

    [Fact]
    public async Task ImportAsync_BasicImport_ReturnsCorrectEntities()
    {
        // Arrange
        var csv = """
            Id,Name,Budget,Balance,Description,SortOrder
            1,Dining Out,100.50,50.25,Food expenses,1
            2,Groceries,200.00,150.00,Weekly groceries,2
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntities, filePath);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Dining Out");
        result[0].Budget.Should().Be(100.50m);
        result[0].Balance.Should().Be(50.25m);
        result[0].Description.Should().Be("Food expenses");
        result[0].SortOrder.Should().Be(1);
        result[1].Id.Should().Be(2);
        result[1].Name.Should().Be("Groceries");
    }

    [Fact]
    public async Task ImportAsync_QuotedStrings_ParsesCorrectly()
    {
        // Arrange
        var csv = """
            Id,Name,Budget,Balance,Description,SortOrder
            1,"Dining Out, Special",100.00,50.00,"Description with, comma",1
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntities, filePath);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Dining Out, Special");
        result[0].Description.Should().Be("Description with, comma");
    }

    [Fact]
    public async Task ImportAsync_QuotedStringsWithEscapedQuotes_ParsesCorrectly()
    {
        // Arrange - using regular string with escaped quotes
        var csv = "Id,Name,Budget,Balance,Description,SortOrder\n1,\"Name with \"\"quotes\"\"\",100.00,50.00,Normal,1";
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntities, filePath);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Name with \"quotes\"");
    }

    [Fact]
    public async Task ImportAsync_CustomSeparator_ParsesCorrectly()
    {
        // Arrange
        var csv = """
            Id;Name;Budget;Balance;Description;SortOrder
            1;Dining Out;100.00;50.00;Food;1
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntities, filePath, ";");

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Dining Out");
    }

    [Fact]
    public async Task ImportAsync_EmptyValues_HandlesNullables()
    {
        // Arrange
        var csv = """
            Id,Name,LastDate,OptionalValue
            1,Test,,
            2,Test2,2023-06-15,42
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntitiesWithNullables, filePath);

        // Assert
        result.Should().HaveCount(2);
        result[0].LastDate.Should().BeNull();
        result[0].OptionalValue.Should().BeNull();
        result[1].LastDate.Should().Be(new DateTime(2023, 6, 15));
        result[1].OptionalValue.Should().Be(42);
    }

    [Fact]
    public async Task ImportAsync_EnumValues_ParsesCorrectly()
    {
        // Arrange
        var csv = """
            Id,Name,Status
            1,First,Active
            2,Second,Inactive
            3,Third,Pending
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntitiesWithEnum, filePath);

        // Assert
        result.Should().HaveCount(3);
        result[0].Status.Should().Be(TestStatus.Active);
        result[1].Status.Should().Be(TestStatus.Inactive);
        result[2].Status.Should().Be(TestStatus.Pending);
    }

    [Fact]
    public async Task ImportAsync_CaseInsensitiveEnumParsing_Works()
    {
        // Arrange
        var csv = """
            Id,Name,Status
            1,First,active
            2,Second,INACTIVE
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntitiesWithEnum, filePath);

        // Assert
        result.Should().HaveCount(2);
        result[0].Status.Should().Be(TestStatus.Active);
        result[1].Status.Should().Be(TestStatus.Inactive);
    }

    [Fact]
    public async Task ImportAsync_FileNotFound_ThrowsArgumentException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_tempDir, "nonexistent.csv");
        using var context = CreateContext();

        // Act & Assert
        var act = () => CsvImportService.ImportAsync(context.TestEntities, nonExistentFile);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*File not found*");
    }

    [Fact]
    public async Task ImportAsync_EmptyFile_ThrowsArgumentException()
    {
        // Arrange
        var filePath = CreateTempFile("");
        using var context = CreateContext();

        // Act & Assert
        var act = () => CsvImportService.ImportAsync(context.TestEntities, filePath);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task ImportAsync_InvalidHeader_ThrowsInvalidOperationException()
    {
        // Arrange
        var csv = """
            Id,InvalidColumn,Budget
            1,Test,100.00
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act & Assert
        var act = () => CsvImportService.ImportAsync(context.TestEntities, filePath);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*InvalidColumn*does not match*");
    }

    [Fact]
    public async Task ImportAsync_CaseInsensitiveHeaders_Works()
    {
        // Arrange - headers with different case
        var csv = """
            ID,NAME,BUDGET,balance,Description,sortorder
            1,Test,100.00,50.00,Desc,1
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntities, filePath);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Test");
    }

    [Fact]
    public async Task ImportAsync_WhitespaceInValues_TrimsCorrectly()
    {
        // Arrange
        var csv = """
            Id,Name,Budget,Balance,Description,SortOrder
            1, Dining Out ,100.00,50.00, Food expenses ,1
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntities, filePath);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Dining Out");
        result[0].Description.Should().Be("Food expenses");
    }

    [Fact]
    public async Task ImportAsync_EmptyLines_AreSkipped()
    {
        // Arrange
        var csv = """
            Id,Name,Budget,Balance,Description,SortOrder
            1,First,100.00,50.00,Desc,1

            2,Second,200.00,100.00,Desc2,2
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntities, filePath);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportAsync_AddsToDbSet()
    {
        // Arrange
        var csv = """
            Id,Name,Budget,Balance,Description,SortOrder
            1,Dining Out,100.00,50.00,Food,1
            """;
        var filePath = CreateTempFile(csv);
        using var context = CreateContext();

        // Act
        await CsvImportService.ImportAsync(context.TestEntities, filePath);
        await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        var entities = await context.TestEntities.ToListAsync(Xunit.TestContext.Current.CancellationToken);
        entities.Should().HaveCount(1);
        entities[0].Name.Should().Be("Dining Out");
    }

    [Fact]
    public async Task ImportAsync_MatchingExampleFromIssue_ParsesCorrectly()
    {
        // This matches the example format from the issue description (but with Id column for EF Core)
        var csv = """
            Id,CategoryId,Name,Budget,Balance,Description,SortOrder
            1,1,Dining Out/Dates,0,0, ,1
            2,1,Gas,0,0, ,1
            """;
        var filePath = CreateTempFile(csv);

        // Create a context that matches the format
        var options = new DbContextOptionsBuilder<TestContextForIssue>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new TestContextForIssue(options);

        // Act
        var result = await CsvImportService.ImportAsync(context.IssueEntities, filePath);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Dining Out/Dates");
        result[0].CategoryId.Should().Be(1);
        result[1].Name.Should().Be("Gas");
    }

    public class IssueTestEntity
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public decimal Balance { get; set; }
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class TestContextForIssue(DbContextOptions<CsvImportServiceTests.TestContextForIssue> options) : DbContext(options)
    {
        public DbSet<IssueTestEntity> IssueEntities => Set<IssueTestEntity>();
  }

  #region List<string> Overload Tests

  [Fact]
    public async Task ImportAsync_FromListOfLines_ReturnsCorrectEntities()
    {
        // Arrange
        var lines = new List<string>
        {
            "Id,Name,Budget,Balance,Description,SortOrder",
            "1,Dining Out,100.50,50.25,Food expenses,1",
            "2,Groceries,200.00,150.00,Weekly groceries,2"
        };
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntities, lines);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Dining Out");
        result[0].Budget.Should().Be(100.50m);
        result[1].Id.Should().Be(2);
        result[1].Name.Should().Be("Groceries");
    }

    [Fact]
    public async Task ImportAsync_FromEmptyList_ThrowsArgumentException()
    {
        // Arrange
        var lines = new List<string>();
        using var context = CreateContext();

        // Act & Assert
        var act = () => CsvImportService.ImportAsync(context.TestEntities, lines);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task ImportAsync_FromListWithCustomSeparator_ParsesCorrectly()
    {
        // Arrange
        var lines = new List<string>
        {
            "Id;Name;Budget;Balance;Description;SortOrder",
            "1;Dining Out;100.00;50.00;Food;1"
        };
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntities, lines, ";");

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Dining Out");
    }

    [Fact]
    public async Task ImportAsync_FromListWithEmptyLines_SkipsEmptyLines()
    {
        // Arrange
        var lines = new List<string>
        {
            "Id,Name,Budget,Balance,Description,SortOrder",
            "1,First,100.00,50.00,Desc,1",
            "",
            "2,Second,200.00,100.00,Desc2,2"
        };
        using var context = CreateContext();

        // Act
        var result = await CsvImportService.ImportAsync(context.TestEntities, lines);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion
}
