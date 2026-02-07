using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Budget.Client.Services;
using Budget.Shared.Models;
using Budget.Shared.Services;
using FluentAssertions;
using Moq;
using Xunit;
using TestContext = Xunit.TestContext;

namespace Budget.Client.Tests.Services;


/// <summary>
/// Tests for EnvelopeDataService
/// </summary>
public class EnvelopeDataServiceTests
{
    private readonly Mock<EnvelopeState> _mockState;
    private readonly Mock<IUserAndOptions> _mockUserOptions;
    private readonly EnvelopeDataService _service;
    private readonly Mock<IBudgetApiClient> _mockApiClient = new();

    public EnvelopeDataServiceTests()
    {
        _mockState = new Mock<EnvelopeState>(null!, null!, null!);
        _mockUserOptions = new Mock<IUserAndOptions>();
        _mockApiClient = new Mock<IBudgetApiClient>();
        _service = new EnvelopeDataService(_mockState.Object, _mockApiClient.Object, _mockUserOptions.Object);
    }

    [Fact]
    public async Task LoadEnvelopeDataAsync_WithCachedData_LoadsFromCache()
    {
        // Arrange
        var categoryDtos = new List<CategoryDto>
    {
      new CategoryDto { CategoryId = "1", Name = "Food", CatType = CatTypes.User, SortOrder = 1 },
      new CategoryDto { CategoryId = "2", Name = "Transport", CatType = CatTypes.User, SortOrder = 2 }
    };

        var envelopeDtos = new List<EnvelopeDto>
    {
      new EnvelopeDto { Id = 1, Name = "Groceries", CategoryId = "1", Balance = 100m, SortOrder = 1, EnvelopeType = EnvelopeTypes.Standard },
      new EnvelopeDto { Id = 2, Name = "Gas", CategoryId = "2", Balance = 50m, SortOrder = 1, EnvelopeType = EnvelopeTypes.Standard }
    };

        var envelopes = new List<EnvelopeResult>
    {
      new EnvelopeResult { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m },
      new EnvelopeResult { EnvelopeId = 2, EnvelopeName = "Gas", CategoryId = "2", Balance = 50m }
    };

        _mockApiClient.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categoryDtos);
        _mockApiClient.Setup(a => a.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>())).ReturnsAsync(envelopeDtos);
        _mockState.Setup(s => s.IsLoaded).Returns(true);
        _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);
        _mockUserOptions.Setup(u => u.Options).Returns(new UserOptions { SelectedCategoryType = "1" });

        // Act
        var result = await _service.LoadEnvelopeDataAsync(false, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.AllEnvelopes.Should().HaveCount(2);
        result.Categories.Should().HaveCount(3); // Includes "All" category
        result.SelectedCategoryId.Should().Be("1");
    }

    [Fact]
    public async Task LoadEnvelopeDataAsync_WithoutCachedData_RefreshesFromApi()
    {
        // Arrange
        var categoryDtos = new List<CategoryDto>
    {
      new CategoryDto { CategoryId = "1", Name = "Food", CatType = CatTypes.User, SortOrder = 1 }
    };

        var envelopeDtos = new List<EnvelopeDto>
    {
      new EnvelopeDto { Id = 1, Name = "Groceries", CategoryId = "1", Balance = 100m, SortOrder = 1, EnvelopeType = EnvelopeTypes.Standard }
    };

        var envelopes = new List<EnvelopeResult>
    {
      new EnvelopeResult { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m }
    };

        _mockApiClient.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categoryDtos);
        _mockApiClient.Setup(a => a.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>())).ReturnsAsync(envelopeDtos);
        _mockState.Setup(s => s.IsLoaded).Returns(false);
        _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);
        _mockUserOptions.Setup(u => u.Options).Returns(new UserOptions { SelectedCategoryType = "0" });

        // Act
        var result = await _service.LoadEnvelopeDataAsync(false , TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Categories.Should().HaveCount(2); // Includes "All" category
    }

    [Fact]
    public async Task LoadEnvelopeDataAsync_WithForceRefresh_SkipsCache()
    {
        // Arrange
        var categoryDtos = new List<CategoryDto>
    {
      new CategoryDto { CategoryId = "1", Name = "Food", CatType = CatTypes.User, SortOrder = 1 }
    };

        var envelopeDtos = new List<EnvelopeDto>();

        _mockApiClient.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categoryDtos);
        _mockApiClient.Setup(a => a.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>())).ReturnsAsync(envelopeDtos);
        _mockState.Setup(s => s.IsLoaded).Returns(true);
        _mockState.Setup(s => s.AllEnvelopeData).Returns(new List<EnvelopeResult>());
        _mockUserOptions.Setup(u => u.Options).Returns(new UserOptions());

        // Act
        var result = await _service.LoadEnvelopeDataAsync(forceRefresh: true, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Categories.Should().HaveCount(2); // Includes "All" category
    }

    [Fact]
    public void ApplyCategoryFilter_WithAllCategoriesSelected_ReturnsFilteredByAvailableCategories()
    {
        // Arrange
        var categories = new List<Cat>
    {
      new Cat { CategoryId = "1", CategoryName = "Food", CatType = CatTypes.User, SortOrder = 1 },
      new Cat { CategoryId = "2", CategoryName = "Transport", CatType = CatTypes.User, SortOrder = 2 }
    };

        var allEnvelopes = new List<EnvelopeResult>
    {
      new EnvelopeResult { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m },
      new EnvelopeResult { EnvelopeId = 2, EnvelopeName = "Gas", CategoryId = "2", Balance = 50m },
      new EnvelopeResult { EnvelopeId = 3, EnvelopeName = "System", CategoryId = "99", Balance = 200m }
    };

        // Act
        var result = _service.ApplyCategoryFilter(allEnvelopes, categories, "0");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.EnvelopeId == 1);
        result.Should().Contain(e => e.EnvelopeId == 2);
        result.Should().NotContain(e => e.EnvelopeId == 3); // System category not in list
    }

    [Fact]
    public void ApplyCategoryFilter_WithSpecificCategory_ReturnsOnlyThatCategory()
    {
        // Arrange
        var categories = new List<Cat>
    {
      new Cat { CategoryId = "1", CategoryName = "Food", CatType = CatTypes.User, SortOrder = 1 },
      new Cat { CategoryId = "2", CategoryName = "Transport", CatType = CatTypes.User, SortOrder = 2 }
    };

        var allEnvelopes = new List<EnvelopeResult>
    {
      new EnvelopeResult { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m },
      new EnvelopeResult { EnvelopeId = 2, EnvelopeName = "Gas", CategoryId = "2", Balance = 50m },
      new EnvelopeResult { EnvelopeId = 3, EnvelopeName = "Dining", CategoryId = "1", Balance = 75m }
    };

        // Act
        var result = _service.ApplyCategoryFilter(allEnvelopes, categories, "1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.EnvelopeId == 1);
        result.Should().Contain(e => e.EnvelopeId == 3);
        result.Should().NotContain(e => e.EnvelopeId == 2);
    }

    [Fact]
    public void ApplyCategoryFilter_WithNullCategoryId_ReturnsAllAvailableCategories()
    {
        // Arrange
        var categories = new List<Cat>
    {
      new Cat { CategoryId = "1", CategoryName = "Food", CatType = CatTypes.User, SortOrder = 1 }
    };

        var allEnvelopes = new List<EnvelopeResult>
    {
      new EnvelopeResult { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m },
      new EnvelopeResult { EnvelopeId = 2, EnvelopeName = "System", CategoryId = "99", Balance = 50m }
    };

        // Act
        var result = _service.ApplyCategoryFilter(allEnvelopes, categories, null);

        // Assert
        result.Should().ContainSingle();
        result[0].EnvelopeId.Should().Be(1);
    }

    [Fact]
    public void UpdateEnvelopeBalances_UpdatesMatchingEnvelopes()
    {
        // Arrange
        var envelopes = new List<EnvelopeResult>
    {
      new EnvelopeResult { EnvelopeId = 1, EnvelopeName = "Groceries", Balance = 100m },
      new EnvelopeResult { EnvelopeId = 2, EnvelopeName = "Gas", Balance = 100m }
    };

        _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);

        var tranResult = new TransactionAddResult();

        tranResult.EnvelopeUpdates = new List<EnvelopeUpdate>
    {
      new EnvelopeUpdate( 1, -150m ),
      new EnvelopeUpdate ( 2,   -75m )
    };

        // Act
        _service.UpdateClientSideEnvelopeBalances(tranResult, envelopes);

        // Assert
        envelopes[0].Balance.Should().Be(-50m);
        envelopes[1].Balance.Should().Be(25m);
    }

    [Fact]
    public void UpdateEnvelopeBalances_WithNonMatchingId_DoesNotThrow()
    {
        // Arrange
        var envelopes = new List<EnvelopeResult>
    {
      new EnvelopeResult { EnvelopeId = 1, EnvelopeName = "Groceries", Balance = 100m }
    };

        _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);

        var tranResult = new TransactionAddResult();


        // Non-existent envelope ID
        tranResult.EnvelopeUpdates = new List<EnvelopeUpdate>
    {
      new EnvelopeUpdate( 999, 200m )
    };
        


        // Act
        var act = () => _service.UpdateClientSideEnvelopeBalances(tranResult, envelopes);

        // Assert
        act.Should().NotThrow();
        envelopes[0].Balance.Should().Be(100m); // Unchanged
    }

    
}