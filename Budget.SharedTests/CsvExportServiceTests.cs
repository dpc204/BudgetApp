using Budget.Shared.Services;
using FluentAssertions;
using Xunit;

namespace Budget.SharedTests;

/// <summary>
/// Unit tests for the CsvExportService.
/// </summary>
public class CsvExportServiceTests
{
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

  #endregion

  [Fact]
  public void ExportToCsv_BasicExport_ReturnsCorrectCsv()
  {
    // Arrange
    var entities = new List<TestEntity>
    {
      new() { Id = 1, Name = "Dining Out", Budget = 100.50m, Balance = 50.25m, Description = "Food expenses", SortOrder = 1 },
      new() { Id = 2, Name = "Groceries", Budget = 200.00m, Balance = 150.00m, Description = "Weekly groceries", SortOrder = 2 }
    };

    // Act
    var result = CsvExportService.ExportToCsv(entities);

    // Assert
    var lines = result.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
    lines[0].Should().Be("Id,Name,Budget,Balance,Description,SortOrder");
    lines[1].Should().Be("1,Dining Out,100.5,50.25,Food expenses,1");
    lines[2].Should().Be("2,Groceries,200,150,Weekly groceries,2");
  }

  [Fact]
  public void ExportToCsv_WithQuotedStrings_EscapesCorrectly()
  {
    // Arrange
    var entities = new List<TestEntity>
    {
      new() { Id = 1, Name = "Dining Out, Special", Budget = 100.00m, Balance = 50.00m, Description = "Description with, comma", SortOrder = 1 }
    };

    // Act
    var result = CsvExportService.ExportToCsv(entities);

    // Assert
    var lines = result.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
    lines[0].Should().Be("Id,Name,Budget,Balance,Description,SortOrder");
    lines[1].Should().Be("1,\"Dining Out, Special\",100,50,\"Description with, comma\",1");
  }

  [Fact]
  public void ExportToCsv_WithQuotesInString_EscapesCorrectly()
  {
    // Arrange
    var entities = new List<TestEntity>
    {
      new() { Id = 1, Name = "Name with \"quotes\"", Budget = 100.00m, Balance = 50.00m, Description = "Normal", SortOrder = 1 }
    };

    // Act
    var result = CsvExportService.ExportToCsv(entities);

    // Assert
    var lines = result.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
    lines[0].Should().Be("Id,Name,Budget,Balance,Description,SortOrder");
    lines[1].Should().Be("1,\"Name with \"\"quotes\"\"\",100,50,Normal,1");
  }

  [Fact]
  public void ExportToCsv_CustomSeparator_UsesCorrectSeparator()
  {
    // Arrange
    var entities = new List<TestEntity>
    {
      new() { Id = 1, Name = "Dining Out", Budget = 100.00m, Balance = 50.00m, Description = "Food", SortOrder = 1 }
    };

    // Act
    var result = CsvExportService.ExportToCsv(entities, ";");

    // Assert
    var lines = result.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
    lines[0].Should().Be("Id;Name;Budget;Balance;Description;SortOrder");
    lines[1].Should().Be("1;Dining Out;100;50;Food;1");
  }

  [Fact]
  public void ExportToCsv_EmptyCollection_ReturnsEmpty()
  {
    // Arrange
    var entities = new List<TestEntity>();

    // Act
    var result = CsvExportService.ExportToCsv(entities);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public void ExportToCsv_WithNullables_HandlesNullsCorrectly()
  {
    // Arrange
    var entities = new List<TestEntityWithNullables>
    {
      new() { Id = 1, Name = "Test", LastDate = null, OptionalValue = null },
      new() { Id = 2, Name = "Test2", LastDate = new DateTime(2023, 6, 15), OptionalValue = 42 }
    };

    // Act
    var result = CsvExportService.ExportToCsv(entities);

    // Assert
    var lines = result.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
    lines[0].Should().Be("Id,Name,LastDate,OptionalValue");
    lines[1].Should().Be("1,Test,,");
    lines[2].Should().Contain("2,Test2,2023-06-15");
    lines[2].Should().Contain(",42");
  }

  [Fact]
  public void ExportToCsv_WithEnums_FormatsCorrectly()
  {
    // Arrange
    var entities = new List<TestEntityWithEnum>
    {
      new() { Id = 1, Name = "First", Status = TestStatus.Active },
      new() { Id = 2, Name = "Second", Status = TestStatus.Inactive },
      new() { Id = 3, Name = "Third", Status = TestStatus.Pending }
    };

    // Act
    var result = CsvExportService.ExportToCsv(entities);

    // Assert
    var lines = result.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
    lines[0].Should().Be("Id,Name,Status");
    lines[1].Should().Be("1,First,Active");
    lines[2].Should().Be("2,Second,Inactive");
    lines[3].Should().Be("3,Third,Pending");
  }

  [Fact]
  public void ExportToCsv_MultipleEntities_FormatsCorrectly()
  {
    // Arrange
    var entities = new List<TestEntity>
    {
      new() { Id = 1, Name = "First", Budget = 100.00m, Balance = 50.00m, Description = "Desc1", SortOrder = 1 },
      new() { Id = 2, Name = "Second", Budget = 200.00m, Balance = 100.00m, Description = "Desc2", SortOrder = 2 },
      new() { Id = 3, Name = "Third", Budget = 300.00m, Balance = 150.00m, Description = "Desc3", SortOrder = 3 }
    };

    // Act
    var result = CsvExportService.ExportToCsv(entities);

    // Assert
    var lines = result.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
    lines.Length.Should().BeGreaterThanOrEqualTo(4); // header + 3 data lines
    lines[0].Should().Be("Id,Name,Budget,Balance,Description,SortOrder");
  }

  [Fact]
  public void ExportToCsv_RoundTrip_CanBeImported()
  {
    // Arrange - create entities and export them
    var originalEntities = new List<TestEntity>
    {
      new() { Id = 1, Name = "Dining Out", Budget = 100.50m, Balance = 50.25m, Description = "Food expenses", SortOrder = 1 },
      new() { Id = 2, Name = "Groceries", Budget = 200.00m, Balance = 150.00m, Description = "Weekly groceries", SortOrder = 2 }
    };

    var csv = CsvExportService.ExportToCsv(originalEntities);

    // Assert - verify the CSV can be parsed back
    var lines = csv.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries);
    lines.Should().HaveCount(3); // header + 2 data lines
    
    // Verify header
    lines[0].Should().Be("Id,Name,Budget,Balance,Description,SortOrder");
    
    // Verify data integrity
    lines[1].Should().Contain("1");
    lines[1].Should().Contain("Dining Out");
    lines[2].Should().Contain("2");
    lines[2].Should().Contain("Groceries");
  }

  [Fact]
  public void ExportToCsv_WithNewlines_EscapesCorrectly()
  {
    // Arrange
    var entities = new List<TestEntity>
    {
      new() { Id = 1, Name = "Test", Budget = 100.00m, Balance = 50.00m, Description = "Line1\nLine2", SortOrder = 1 }
    };

    // Act
    var result = CsvExportService.ExportToCsv(entities);

    // Assert
    // When a field contains a newline, it should be wrapped in quotes
    result.Should().Contain("\"Line1\nLine2\"");
  }
}
