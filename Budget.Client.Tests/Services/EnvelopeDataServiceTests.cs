using Budget.Client.Services;

namespace Budget.Client.Tests.Services;

/// <summary>
/// Tests for EnvelopeDataService
/// </summary>
public class EnvelopeDataServiceTests
{
  private readonly Mock<EnvelopeState> _mockState;
  private readonly Mock<IUserAndOptions> _mockUserOptions;
  private readonly EnvelopeDataService _service;

  public EnvelopeDataServiceTests()
  {
    _mockState = new Mock<EnvelopeState>(null!, null!, null!);
    _mockUserOptions = new Mock<IUserAndOptions>();
    _service = new EnvelopeDataService(_mockState.Object, _mockUserOptions.Object);
  }

  [Fact]
  public async Task LoadEnvelopeDataAsync_WithCachedData_LoadsFromCache()
  {
    // Arrange
    var categories = new List<Cat>
    {
      new Cat { CategoryId = "1", CategoryName = "Food", CatType = CatTypes.User, SortOrder = 1 },
      new Cat { CategoryId = "2", CategoryName = "Transport", CatType = CatTypes.User, SortOrder = 2 }
    };

    var envelopes = new List<EnvelopeResult>
    {
      new EnvelopeResult { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m },
      new EnvelopeResult { EnvelopeId = 2, EnvelopeName = "Gas", CategoryId = "2", Balance = 50m }
    };

    _mockState.Setup(s => s.IsLoaded).Returns(true);
    _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);
    _mockState.Setup(s => s.Cats).Returns(categories);
    _mockUserOptions.Setup(u => u.Options).Returns(new UserOptions { SelectedCategoryType = "1" });

    // Act
    var result = await _service.LoadEnvelopeDataAsync();

    // Assert
    result.Should().NotBeNull();
    result.AllEnvelopes.Should().HaveCount(2);
    result.Categories.Should().HaveCount(2);
    result.SelectedCategoryId.Should().Be("1");
    _mockState.Verify(s => s.TryLoadFromCacheAsync(), Times.Once);
    _mockState.Verify(s => s.RefreshAsync(), Times.Never);
  }

  [Fact]
  public async Task LoadEnvelopeDataAsync_WithoutCachedData_RefreshesFromApi()
  {
    // Arrange
    var categories = new List<Cat>
    {
      new Cat { CategoryId = "1", CategoryName = "Food", CatType = CatTypes.User, SortOrder = 1 }
    };

    var envelopes = new List<EnvelopeResult>
    {
      new EnvelopeResult { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m }
    };

    _mockState.Setup(s => s.IsLoaded).Returns(false);
    _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);
    _mockState.Setup(s => s.Cats).Returns(categories);
    _mockUserOptions.Setup(u => u.Options).Returns(new UserOptions { SelectedCategoryType = "0" });

    // Act
    var result = await _service.LoadEnvelopeDataAsync();

    // Assert
    result.Should().NotBeNull();
    // When IsLoaded is false, it goes directly to RefreshAsync without trying cache first
    _mockState.Verify(s => s.TryLoadFromCacheAsync(), Times.Never);
    _mockState.Verify(s => s.RefreshAsync(), Times.Once);
  }

  [Fact]
  public async Task LoadEnvelopeDataAsync_WithForceRefresh_SkipsCache()
  {
    // Arrange
    var categories = new List<Cat>
    {
      new Cat { CategoryId = "1", CategoryName = "Food", CatType = CatTypes.User, SortOrder = 1 }
    };

    _mockState.Setup(s => s.IsLoaded).Returns(true);
    _mockState.Setup(s => s.AllEnvelopeData).Returns(new List<EnvelopeResult>());
    _mockState.Setup(s => s.Cats).Returns(categories);
    _mockUserOptions.Setup(u => u.Options).Returns(new UserOptions());

    // Act
    var result = await _service.LoadEnvelopeDataAsync(forceRefresh: true);

    // Assert
    _mockState.Verify(s => s.TryLoadFromCacheAsync(), Times.Never);
    _mockState.Verify(s => s.RefreshAsync(), Times.Once);
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
      new EnvelopeResult { EnvelopeId = 2, EnvelopeName = "Gas", Balance = 50m }
    };

    _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);

    var updatedEnvelopes = new List<EnvelopeDto>
    {
      new EnvelopeDto { Id = 1, Name = "Groceries", Balance = 150m },
      new EnvelopeDto { Id = 2, Name = "Gas", Balance = 75m }
    };

    // Act
    _service.UpdateEnvelopeBalances(updatedEnvelopes);

    // Assert
    envelopes[0].Balance.Should().Be(150m);
    envelopes[1].Balance.Should().Be(75m);
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

    var updatedEnvelopes = new List<EnvelopeDto>
    {
      new EnvelopeDto { Id = 999, Name = "NonExistent", Balance = 200m }
    };

    // Act
    var act = () => _service.UpdateEnvelopeBalances(updatedEnvelopes);

    // Assert
    act.Should().NotThrow();
    envelopes[0].Balance.Should().Be(100m); // Unchanged
  }

  [Fact]
  public async Task SaveStateAsync_CallsStateSave()
  {
    // Act
    await _service.SaveStateAsync();

    // Assert
    _mockState.Verify(s => s.SaveAsync(), Times.Once);
  }

  [Fact]
  public async Task RefreshAsync_CallsStateRefresh()
  {
    // Act
    await _service.RefreshAsync();

    // Assert
    _mockState.Verify(s => s.RefreshAsync(), Times.Once);
  }

  [Fact]
  public void GetCategoriesForSelect_ReturnsStateCategories()
  {
    // Arrange
    var categories = new List<Cat>
    {
      new Cat { CategoryId = "1", CategoryName = "Food", CatType = CatTypes.User, SortOrder = 1 }
    };

    _mockState.Setup(s => s.Cats).Returns(categories);

    // Act
    var result = _service.GetCategoriesForSelect();

    // Assert
    result.Should().BeSameAs(categories);
  }
}
