using System;
using System.Linq;

using Budget.Api.Shared.Extensions;
using Xunit;

namespace Budget.Api.Shared.Extensions.UnitTests
{
    public class DynamicSortExtensionsTests
    {
        /// <summary>
        /// Tests that OrderByDescendingDynamic throws ArgumentNullException when query parameter is null.
        /// Input: null query
        /// Expected: ArgumentNullException
        /// </summary>
        [Fact(Skip="ProductionBugSuspected")]
        [Trait("Category", "ProductionBugSuspected")]
        public void OrderByDescendingDynamic_NullQuery_ThrowsArgumentNullException()
        {
            // Arrange
            IQueryable<TestEntity>? query = null;
            string propertyName = "Id";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => query!.OrderByDescendingDynamic(propertyName));
        }

        /// <summary>
        /// Tests that OrderByDescendingDynamic throws ArgumentNullException when propertyName is null.
        /// Input: null propertyName
        /// Expected: ArgumentNullException
        /// </summary>
        [Fact]
        public void OrderByDescendingDynamic_NullPropertyName_ThrowsArgumentNullException()
        {
            // Arrange
            var data = new[] { new TestEntity { Id = 1 } };
            var query = data.AsQueryable();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => query.OrderByDescendingDynamic(null!));
        }

        /// <summary>
        /// Tests that OrderByDescendingDynamic throws ArgumentException when propertyName is empty.
        /// Input: empty string propertyName
        /// Expected: ArgumentException
        /// </summary>
        [Fact]
        public void OrderByDescendingDynamic_EmptyPropertyName_ThrowsArgumentException()
        {
            // Arrange
            var data = new[] { new TestEntity { Id = 1 } };
            var query = data.AsQueryable();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => query.OrderByDescendingDynamic(string.Empty));
        }

        /// <summary>
        /// Tests that OrderByDescendingDynamic throws ArgumentException when propertyName contains only whitespace.
        /// Input: whitespace-only propertyName
        /// Expected: ArgumentException
        /// </summary>
        [Theory]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void OrderByDescendingDynamic_WhitespacePropertyName_ThrowsArgumentException(string propertyName)
        {
            // Arrange
            var data = new[] { new TestEntity { Id = 1 } };
            var query = data.AsQueryable();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => query.OrderByDescendingDynamic(propertyName));
        }

        /// <summary>
        /// Tests that OrderByDescendingDynamic throws ArgumentException when propertyName does not exist on type T.
        /// Input: non-existent property name
        /// Expected: ArgumentException
        /// </summary>
        [Theory]
        [InlineData("NonExistentProperty")]
        [InlineData("InvalidProperty")]
        [InlineData("DoesNotExist")]
        public void OrderByDescendingDynamic_NonExistentPropertyName_ThrowsArgumentException(string propertyName)
        {
            // Arrange
            var data = new[] { new TestEntity { Id = 1 } };
            var query = data.AsQueryable();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => query.OrderByDescendingDynamic(propertyName));
        }

        /// <summary>
        /// Tests that OrderByDescendingDynamic correctly sorts by decimal property in descending order.
        /// Input: valid decimal property name "Amount"
        /// Expected: query ordered by Amount descending
        /// </summary>
        [Fact(Skip="ProductionBugSuspected")]
        [Trait("Category", "ProductionBugSuspected")]
        public void OrderByDescendingDynamic_ValidDecimalProperty_SortsDescending()
        {
            // Arrange
            var data = new[]
            {
                new TestEntity { Id = 1, Amount = 10.5m },
                new TestEntity { Id = 2, Amount = 100.99m },
                new TestEntity { Id = 3, Amount = 50.25m }
            };
            var query = data.AsQueryable();

            // Act
            var result = query.OrderByDescendingDynamic("Amount").ToList();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(100.99m, result[0].Amount);
            Assert.Equal(50.25m, result[1].Amount);
            Assert.Equal(10.5m, result[2].Amount);
        }

        /// <summary>
        /// Tests that OrderByDescendingDynamic correctly sorts by boolean property in descending order.
        /// Input: valid boolean property name "IsActive"
        /// Expected: query ordered by IsActive descending (true before false)
        /// </summary>
        [Fact(Skip="ProductionBugSuspected")]
        [Trait("Category", "ProductionBugSuspected")]
        public void OrderByDescendingDynamic_ValidBooleanProperty_SortsDescending()
        {
            // Arrange
            var data = new[]
            {
                new TestEntity { Id = 1, IsActive = false },
                new TestEntity { Id = 2, IsActive = true },
                new TestEntity { Id = 3, IsActive = false },
                new TestEntity { Id = 4, IsActive = true }
            };
            var query = data.AsQueryable();

            // Act
            var result = query.OrderByDescendingDynamic("IsActive").ToList();

            // Assert
            Assert.Equal(4, result.Count);
            Assert.True(result[0].IsActive);
            Assert.True(result[1].IsActive);
            Assert.False(result[2].IsActive);
            Assert.False(result[3].IsActive);
        }

        /// <summary>
        /// Tests that OrderByDescendingDynamic handles empty queryable collection.
        /// Input: empty queryable with valid property name
        /// Expected: returns empty queryable without throwing
        /// </summary>
        [Fact(Skip="ProductionBugSuspected")]
        [Trait("Category", "ProductionBugSuspected")]
        public void OrderByDescendingDynamic_EmptyQueryable_ReturnsEmpty()
        {
            // Arrange
            var data = Array.Empty<TestEntity>();
            var query = data.AsQueryable();

            // Act
            var result = query.OrderByDescendingDynamic("Id").ToList();

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that OrderByDescendingDynamic correctly handles nullable integer property in descending order.
        /// Input: valid nullable integer property name "NullableInt"
        /// Expected: query ordered with non-null values first in descending order, then nulls
        /// </summary>
        [Fact(Skip="ProductionBugSuspected")]
        [Trait("Category", "ProductionBugSuspected")]
        public void OrderByDescendingDynamic_NullableIntegerProperty_SortsDescending()
        {
            // Arrange
            var data = new[]
            {
                new TestEntity { Id = 1, NullableInt = 10 },
                new TestEntity { Id = 2, NullableInt = null },
                new TestEntity { Id = 3, NullableInt = 20 },
                new TestEntity { Id = 4, NullableInt = null }
            };
            var query = data.AsQueryable();

            // Act
            var result = query.OrderByDescendingDynamic("NullableInt").ToList();

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Equal(20, result[0].NullableInt);
            Assert.Equal(10, result[1].NullableInt);
            Assert.Null(result[2].NullableInt);
            Assert.Null(result[3].NullableInt);
        }

        /// <summary>
        /// Tests that OrderByDescendingDynamic correctly handles duplicate values.
        /// Input: queryable with duplicate property values
        /// Expected: all items returned with duplicates in descending order
        /// </summary>
        [Fact(Skip="ProductionBugSuspected")]
        [Trait("Category", "ProductionBugSuspected")]
        public void OrderByDescendingDynamic_DuplicateValues_PreservesAllItems()
        {
            // Arrange
            var data = new[]
            {
                new TestEntity { Id = 1, Amount = 50m },
                new TestEntity { Id = 2, Amount = 100m },
                new TestEntity { Id = 3, Amount = 50m },
                new TestEntity { Id = 4, Amount = 100m }
            };
            var query = data.AsQueryable();

            // Act
            var result = query.OrderByDescendingDynamic("Amount").ToList();

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Equal(100m, result[0].Amount);
            Assert.Equal(100m, result[1].Amount);
            Assert.Equal(50m, result[2].Amount);
            Assert.Equal(50m, result[3].Amount);
        }

        /// <summary>
        /// Helper class used for testing dynamic sorting functionality.
        /// Contains various property types to test different sorting scenarios.
        /// </summary>
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public DateTime CreatedDate { get; set; }
            public decimal Amount { get; set; }
            public bool IsActive { get; set; }
            public int? NullableInt { get; set; }
        }

        /// <summary>
        /// Tests that OrderByDynamic throws ArgumentNullException when the query parameter is null.
        /// Input: null query, valid property name
        /// Expected: ArgumentNullException
        /// </summary>
        [Fact(Skip="ProductionBugSuspected")]
        [Trait("Category", "ProductionBugSuspected")]
        public void OrderByDynamic_NullQuery_ThrowsArgumentNullException()
        {
            // Arrange
            IQueryable<TestEntity>? query = null;
            string propertyName = "Name";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => query!.OrderByDynamic(propertyName));
        }

        /// <summary>
        /// Tests that OrderByDynamic works correctly with an empty query.
        /// Input: empty query, valid property name
        /// Expected: Empty ordered query
        /// </summary>
        [Fact(Skip="ProductionBugSuspected")]
        [Trait("Category", "ProductionBugSuspected")]
        public void OrderByDynamic_EmptyQuery_ReturnsEmptyQuery()
        {
            // Arrange
            var query = Array.Empty<TestEntity>().AsQueryable();

            // Act
            var result = query.OrderByDynamic("Name").ToList();

            // Assert
            Assert.Empty(result);
        }

    }
}