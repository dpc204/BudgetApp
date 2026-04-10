using Budget.Client.Services;
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
  private readonly Mock<IEnvelopesApiClient> _mockEnvelopesClient = new();
  private readonly Mock<ICategoriesApiClient> _mockCategoriesClient = new();

  public EnvelopeDataServiceTests()
  {
    _mockState = new Mock<EnvelopeState>(null!, null!, null!);
    _mockUserOptions = new Mock<IUserAndOptions>();
    _mockEnvelopesClient = new Mock<IEnvelopesApiClient>();
    _mockCategoriesClient = new Mock<ICategoriesApiClient>();
    _service = new EnvelopeDataService(_mockEnvelopesClient.Object, _mockCategoriesClient.Object, _mockUserOptions.Object);
  }

  [Fact]
  public async Task LoadEnvelopeDataAsync_WithCachedData_LoadsFromCache()
  {
    // Arrange
    var categoryDtos = new List<CategoryDto>
    {
      new() { CategoryId = "1", Name = "Food", CatType = CatTypes.User, SortOrder = 1 },
      new() { CategoryId = "2", Name = "Transport", CatType = CatTypes.User, SortOrder = 2 }
    };

    var envelopeDtos = new List<EnvelopeDto>
    {
      new()
      {
        Id = 1, Name = "Groceries", CategoryId = "1", Balance = 100m, SortOrder = 1,
        EnvelopeType = EnvelopeTypes.Standard
      },
      new()
      {
        Id = 2, Name = "Gas", CategoryId = "2", Balance = 50m, SortOrder = 1, EnvelopeType = EnvelopeTypes.Standard
      }
    };

    var envelopes = new List<EnvelopeResult>
    {
      new() { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m },
      new() { EnvelopeId = 2, EnvelopeName = "Gas", CategoryId = "2", Balance = 50m }
    };


    //mock _mockUserOptions.Options to return "1" for SelectedCategoryId
    _mockUserOptions.Setup(uo => uo.Options).Returns(new UserOptions { SelectedCategoryType = "ALL" });

    _mockCategoriesClient.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categoryDtos);
    _mockEnvelopesClient.Setup(a => a.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(envelopeDtos);
    _mockState.Setup(s => s.IsLoaded).Returns(true);
    _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);

    // Act
    var result = await _service.LoadEnvelopeDataAsync(false, TestContext.Current.CancellationToken);

    // Assert
    result.Should().NotBeNull();
    result.AllEnvelopes.Should().HaveCount(2);
    result.Categories.Should().HaveCount(3); // Includes "All" category
    result.SelectedCategoryId.Should().Be("ALL");
  }

  [Fact]
  public async Task LoadEnvelopeDataAsync_WithoutCachedData_RefreshesFromApi()
  {
    // Arrange
    var categoryDtos = new List<CategoryDto>
    {
      new() { CategoryId = "1", Name = "Food", CatType = CatTypes.User, SortOrder = 1 }
    };

    var envelopeDtos = new List<EnvelopeDto>
    {
      new()
      {
        Id = 1, Name = "Groceries", CategoryId = "1", Balance = 100m, SortOrder = 1,
        EnvelopeType = EnvelopeTypes.Standard
      }
    };

    var envelopes = new List<EnvelopeResult>
    {
      new() { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m }
    };

    _mockCategoriesClient.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categoryDtos);
    _mockEnvelopesClient.Setup(a => a.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(envelopeDtos);
    _mockState.Setup(s => s.IsLoaded).Returns(false);
    _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);
    _mockUserOptions.Setup(uo => uo.Options).Returns(new UserOptions { SelectedCategoryType = "ALL" });

    // Act
    var result = await _service.LoadEnvelopeDataAsync(false, TestContext.Current.CancellationToken);

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
      new() { CategoryId = "1", Name = "Food", CatType = CatTypes.User, SortOrder = 1 }
    };

    var envelopeDtos = new List<EnvelopeDto>();

    _mockCategoriesClient.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categoryDtos);
    _mockEnvelopesClient.Setup(a => a.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(envelopeDtos);
    _mockState.Setup(s => s.IsLoaded).Returns(true);
    _mockState.Setup(s => s.AllEnvelopeData).Returns([]);
    _mockUserOptions.Setup(uo => uo.Options).Returns(new UserOptions { SelectedCategoryType = "ALL" });

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
      new() { CategoryId = "1", CategoryName = "Food", CatType = CatTypes.User, SortOrder = 1 },
      new() { CategoryId = "2", CategoryName = "Transport", CatType = CatTypes.User, SortOrder = 2 }
    };

    var allEnvelopes = new List<EnvelopeResult>
    {
      new() { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m },
      new() { EnvelopeId = 2, EnvelopeName = "Gas", CategoryId = "2", Balance = 50m },
      new() { EnvelopeId = 3, EnvelopeName = "System", CategoryId = "99", Balance = 200m }
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
      new() { CategoryId = "1", CategoryName = "Food", CatType = CatTypes.User, SortOrder = 1 },
      new() { CategoryId = "2", CategoryName = "Transport", CatType = CatTypes.User, SortOrder = 2 }
    };

    var allEnvelopes = new List<EnvelopeResult>
    {
      new() { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m },
      new() { EnvelopeId = 2, EnvelopeName = "Gas", CategoryId = "2", Balance = 50m },
      new() { EnvelopeId = 3, EnvelopeName = "Dining", CategoryId = "1", Balance = 75m }
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
      new() { CategoryId = "1", CategoryName = "Food", CatType = CatTypes.User, SortOrder = 1 }
    };

    var allEnvelopes = new List<EnvelopeResult>
    {
      new() { EnvelopeId = 1, EnvelopeName = "Groceries", CategoryId = "1", Balance = 100m },
      new() { EnvelopeId = 2, EnvelopeName = "System", CategoryId = "99", Balance = 50m }
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
      new() { EnvelopeId = 1, EnvelopeName = "Groceries", Balance = 100m },
      new() { EnvelopeId = 2, EnvelopeName = "Gas", Balance = 100m }
    };

    _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);

    var tranResult = new EnvelopeDeltas
    {
      new(1, -150m),
      new(2, -75m)
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
      new() { EnvelopeId = 1, EnvelopeName = "Groceries", Balance = 100m }
    };

    _mockState.Setup(s => s.AllEnvelopeData).Returns(envelopes);

    var tranResult = new EnvelopeDeltas
    {
      new(999, 200m)
    };


    // Act
    var act = () => _service.UpdateClientSideEnvelopeBalances(tranResult, envelopes);

    // Assert
    act.Should().NotThrow();
    envelopes[0].Balance.Should().Be(100m); // Unchanged
  }
}