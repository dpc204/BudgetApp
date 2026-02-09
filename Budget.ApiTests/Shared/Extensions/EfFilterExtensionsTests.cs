using System;
using System.Collections.Generic;
using System.Linq;
using Budget.Api.Shared.Extensions;
using Budget.Shared.Models.Queries;
using Xunit;


namespace Budget.Api.Shared.Extensions.UnitTests;

/// <summary>
/// Tests for the EfFilterExtensions class.
/// </summary>
public class EfFilterExtensionsTests
{
    /// <summary>
    /// Tests that ApplyFilters returns the original query when filters parameter is null.
    /// Input: null filters list
    /// Expected: Original query returned unchanged
    /// </summary>
    [Fact]
    public void ApplyFilters_NullFilters_ReturnsOriginalQuery()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 }
        };
        var query = data.AsQueryable();

        // Act
        var result = query.ApplyFilters(null);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Same(query, result);
    }

    /// <summary>
    /// Tests that ApplyFilters returns the original query when filters list is empty.
    /// Input: empty filters list
    /// Expected: Original query returned unchanged
    /// </summary>
    [Fact]
    public void ApplyFilters_EmptyFilters_ReturnsOriginalQuery()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>();

        // Act
        var result = query.ApplyFilters(filters);

        // Assert
        Assert.Equal(2, result.Count());
    }

    /// <summary>
    /// Tests that ApplyFilters skips filters with null Column property.
    /// Input: filter with null Column
    /// Expected: Filter is skipped, all records returned
    /// </summary>
    [Fact]
    public void ApplyFilters_NullColumn_SkipsFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = null, Operator = "=", Value = "Test1" }
        };

        // Act
        var result = query.ApplyFilters(filters);

        // Assert
        Assert.Equal(2, result.Count());
    }

    /// <summary>
    /// Tests that ApplyFilters skips filters with empty Column property.
    /// Input: filter with empty string Column
    /// Expected: Filter is skipped, all records returned
    /// </summary>
    [Fact]
    public void ApplyFilters_EmptyColumn_SkipsFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "", Operator = "=", Value = "Test1" }
        };

        // Act
        var result = query.ApplyFilters(filters);

        // Assert
        Assert.Equal(2, result.Count());
    }

    /// <summary>
    /// Tests that ApplyFilters skips filters with whitespace-only Column property.
    /// Input: filter with whitespace Column
    /// Expected: Filter is skipped, all records returned
    /// </summary>
    [Fact]
    public void ApplyFilters_WhitespaceColumn_SkipsFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "   ", Operator = "=", Value = "Test1" }
        };

        // Act
        var result = query.ApplyFilters(filters);

        // Assert
        Assert.Equal(2, result.Count());
    }

    /// <summary>
    /// Tests that ApplyFilters skips filters with null Value property.
    /// Input: filter with null Value
    /// Expected: Filter is skipped, all records returned
    /// </summary>
    [Fact]
    public void ApplyFilters_NullValue_SkipsFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "=", Value = null }
        };

        // Act
        var result = query.ApplyFilters(filters);

        // Assert
        Assert.Equal(2, result.Count());
    }

    /// <summary>
    /// Tests that ApplyFilters skips filters with empty Value property.
    /// Input: filter with empty string Value
    /// Expected: Filter is skipped, all records returned
    /// </summary>
    [Fact]
    public void ApplyFilters_EmptyValue_SkipsFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "=", Value = "" }
        };

        // Act
        var result = query.ApplyFilters(filters);

        // Assert
        Assert.Equal(2, result.Count());
    }

    /// <summary>
    /// Tests that ApplyFilters skips filters with whitespace-only Value property.
    /// Input: filter with whitespace Value
    /// Expected: Filter is skipped, all records returned
    /// </summary>
    [Fact]
    public void ApplyFilters_WhitespaceValue_SkipsFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "=", Value = "   " }
        };

        // Act
        var result = query.ApplyFilters(filters);

        // Assert
        Assert.Equal(2, result.Count());
    }

    /// <summary>
    /// Tests that ApplyFilters skips filters referencing non-existent properties.
    /// Input: filter with non-existent property name
    /// Expected: Filter is skipped, all records returned
    /// </summary>
    [Fact]
    public void ApplyFilters_NonExistentProperty_SkipsFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "NonExistentProperty", Operator = "=", Value = "SomeValue" }
        };

        // Act
        var result = query.ApplyFilters(filters);

        // Assert
        Assert.Equal(2, result.Count());
    }

    /// <summary>
    /// Tests that ApplyFilters applies equality filter when operator is not specified or is default.
    /// Input: filter with no operator or default operator on string property
    /// Expected: Only matching record returned
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("=")]
    [InlineData("equals")]
    public void ApplyFilters_DefaultOperator_AppliesEqualityFilter(string? operatorValue)
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 },
            new() { Id = 3, Name = "Test1", Age = 35 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = operatorValue, Value = "Test1" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal("Test1", item.Name));
    }

    /// <summary>
    /// Tests that ApplyFilters applies contains filter with lowercase "contains" operator.
    /// Input: filter with "contains" operator on string property
    /// Expected: Records containing substring returned
    /// </summary>
    [Fact]
    public void ApplyFilters_ContainsOperatorLowercase_AppliesContainsFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestValue1", Age = 25 },
            new() { Id = 2, Name = "SomethingElse", Age = 30 },
            new() { Id = 3, Name = "AnotherTest", Age = 35 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "contains", Value = "Test" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Name == "TestValue1");
        Assert.Contains(result, item => item.Name == "AnotherTest");
    }

    /// <summary>
    /// Tests that ApplyFilters applies contains filter with Pascal case "Contains" operator.
    /// Input: filter with "Contains" operator on string property
    /// Expected: Records containing substring returned
    /// </summary>
    [Fact]
    public void ApplyFilters_ContainsOperatorPascalCase_AppliesContainsFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestValue1", Age = 25 },
            new() { Id = 2, Name = "SomethingElse", Age = 30 },
            new() { Id = 3, Name = "AnotherTest", Age = 35 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "Contains", Value = "Test" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Name == "TestValue1");
        Assert.Contains(result, item => item.Name == "AnotherTest");
    }

    /// <summary>
    /// Tests that ApplyFilters applies StartsWith filter with lowercase "starts" operator.
    /// Input: filter with "starts" operator on string property
    /// Expected: Records starting with substring returned
    /// </summary>
    [Fact]
    public void ApplyFilters_StartsOperatorLowercase_AppliesStartsWithFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestValue1", Age = 25 },
            new() { Id = 2, Name = "SomethingElse", Age = 30 },
            new() { Id = 3, Name = "TestAnother", Age = 35 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "starts", Value = "Test" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Name == "TestValue1");
        Assert.Contains(result, item => item.Name == "TestAnother");
    }

    /// <summary>
    /// Tests that ApplyFilters applies StartsWith filter with Pascal case "StartsWith" operator.
    /// Input: filter with "StartsWith" operator on string property
    /// Expected: Records starting with substring returned
    /// </summary>
    [Fact]
    public void ApplyFilters_StartsWithOperatorPascalCase_AppliesStartsWithFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestValue1", Age = 25 },
            new() { Id = 2, Name = "SomethingElse", Age = 30 },
            new() { Id = 3, Name = "TestAnother", Age = 35 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "StartsWith", Value = "Test" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Name == "TestValue1");
        Assert.Contains(result, item => item.Name == "TestAnother");
    }

    /// <summary>
    /// Tests that ApplyFilters applies EndsWith filter with lowercase "ends" operator.
    /// Input: filter with "ends" operator on string property
    /// Expected: Records ending with substring returned
    /// </summary>
    [Fact]
    public void ApplyFilters_EndsOperatorLowercase_AppliesEndsWithFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "ValueTest", Age = 25 },
            new() { Id = 2, Name = "SomethingElse", Age = 30 },
            new() { Id = 3, Name = "AnotherTest", Age = 35 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "ends", Value = "Test" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Name == "ValueTest");
        Assert.Contains(result, item => item.Name == "AnotherTest");
    }

    /// <summary>
    /// Tests that ApplyFilters applies EndsWith filter with Pascal case "EndsWith" operator.
    /// Input: filter with "EndsWith" operator on string property
    /// Expected: Records ending with substring returned
    /// </summary>
    [Fact]
    public void ApplyFilters_EndsWithOperatorPascalCase_AppliesEndsWithFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "ValueTest", Age = 25 },
            new() { Id = 2, Name = "SomethingElse", Age = 30 },
            new() { Id = 3, Name = "AnotherTest", Age = 35 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "EndsWith", Value = "Test" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Name == "ValueTest");
        Assert.Contains(result, item => item.Name == "AnotherTest");
    }

    /// <summary>
    /// Tests that ApplyFilters applies equality filter on integer property.
    /// Input: filter on integer property with string value
    /// Expected: Records with matching integer value returned
    /// </summary>
    [Fact]
    public void ApplyFilters_IntegerProperty_AppliesFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 },
            new() { Id = 3, Name = "Test3", Age = 25 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Age", Operator = "=", Value = "25" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal(25, item.Age));
    }

    /// <summary>
    /// Tests that ApplyFilters applies equality filter on decimal property.
    /// Input: filter on decimal property with string value
    /// Expected: Records with matching decimal value returned
    /// </summary>
    [Fact]
    public void ApplyFilters_DecimalProperty_AppliesFilter()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Price = 19.99m },
            new() { Id = 2, Name = "Test2", Price = 29.99m },
            new() { Id = 3, Name = "Test3", Price = 19.99m }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Price", Operator = "=", Value = "19.99" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal(19.99m, item.Price));
    }

    /// <summary>
    /// Tests that ApplyFilters applies equality filter on boolean property.
    /// Input: filter on boolean property with string value
    /// Expected: Records with matching boolean value returned
    /// </summary>
    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    public void ApplyFilters_BooleanProperty_AppliesFilter(string value, bool expectedValue)
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", IsActive = true },
            new() { Id = 2, Name = "Test2", IsActive = false },
            new() { Id = 3, Name = "Test3", IsActive = true }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "IsActive", Operator = "=", Value = value }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.All(result, item => Assert.Equal(expectedValue, item.IsActive));
    }

    /// <summary>
    /// Tests that ApplyFilters applies multiple filters with AND logic.
    /// Input: multiple valid filters
    /// Expected: Only records matching all filters returned
    /// </summary>
    [Fact]
    public void ApplyFilters_MultipleFilters_AppliesAllWithAndLogic()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestValue1", Age = 25, IsActive = true },
            new() { Id = 2, Name = "TestValue2", Age = 30, IsActive = false },
            new() { Id = 3, Name = "OtherValue", Age = 25, IsActive = true }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "contains", Value = "Test" },
            new() { Column = "Age", Operator = "=", Value = "25" },
            new() { Column = "IsActive", Operator = "=", Value = "true" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    /// <summary>
    /// Tests that ApplyFilters applies only valid filters when some filters are invalid.
    /// Input: mix of valid and invalid filters
    /// Expected: Only valid filters applied, invalid ones skipped
    /// </summary>
    [Fact]
    public void ApplyFilters_MixOfValidAndInvalidFilters_AppliesOnlyValid()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 },
            new() { Id = 3, Name = "Test1", Age = 35 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = null, Operator = "=", Value = "Something" },
            new() { Column = "Name", Operator = "=", Value = "Test1" },
            new() { Column = "NonExistent", Operator = "=", Value = "Value" },
            new() { Column = "Age", Operator = "=", Value = "" }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal("Test1", item.Name));
    }

    /// <summary>
    /// Tests that ApplyFilters throws exception when type conversion fails.
    /// Input: filter with invalid value for integer property
    /// Expected: FormatException thrown during query execution
    /// </summary>
    [Fact]
    public void ApplyFilters_InvalidTypeConversion_ThrowsException()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Age", Operator = "=", Value = "NotANumber" }
        };

        // Act & Assert
        Assert.Throws<FormatException>(() => query.ApplyFilters(filters));
    }

    /// <summary>
    /// Tests that ApplyFilters throws exception when string operator is used on non-string property.
    /// Input: Contains operator on integer property
    /// Expected: Exception thrown during query execution
    /// </summary>
    [Fact]
    public void ApplyFilters_StringOperatorOnNonStringProperty_ThrowsException()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Age", Operator = "contains", Value = "25" }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => query.ApplyFilters(filters));
    }

    /// <summary>
    /// Tests that ApplyFilters preserves deferred execution of IQueryable.
    /// Input: valid filter
    /// Expected: Filter is not executed until query is materialized
    /// </summary>
    [Fact]
    public void ApplyFilters_DeferredExecution_FiltersAppliedOnMaterialization()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = 25 },
            new() { Id = 2, Name = "Test2", Age = 30 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Name", Operator = "=", Value = "Test1" }
        };

        // Act
        var result = query.ApplyFilters(filters);

        // Assert - query not materialized yet
        Assert.IsAssignableFrom<IQueryable<TestEntity>>(result);

        // Materialize and verify
        var materializedResult = result.ToList();
        Assert.Single(materializedResult);
        Assert.Equal("Test1", materializedResult[0].Name);
    }

    /// <summary>
    /// Tests that ApplyFilters handles extreme integer boundary values.
    /// Input: filters with int.MinValue and int.MaxValue
    /// Expected: Filters applied correctly with boundary values
    /// </summary>
    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    public void ApplyFilters_IntegerBoundaryValues_AppliesCorrectly(int value)
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Age = value },
            new() { Id = 2, Name = "Test2", Age = value + 1 }
        };
        var query = data.AsQueryable();
        var filters = new List<FilterItem>
        {
            new() { Column = "Age", Operator = "=", Value = value.ToString() }
        };

        // Act
        var result = query.ApplyFilters(filters).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(value, result[0].Age);
    }

    /// <summary>
    /// Test entity class used for testing filter operations.
    /// </summary>
    private class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }
}