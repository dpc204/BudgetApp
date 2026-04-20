using Bunit;
using MudBlazor.Extensions;
using TestContext = Bunit.TestContext;

namespace Budget.Client.Tests.Components.Forms;

/// <summary>
/// Tests for EnvelopePicker component
/// </summary>
public class EnvelopePickerTests : BunitContext
{
  private readonly Mock<IEnvelopesApiClient> _mockEnvelopesClient;
  private readonly Mock<ICategoriesApiClient> _mockCategoriesClient;

  public EnvelopePickerTests()
  {
    _mockEnvelopesClient = new Mock<IEnvelopesApiClient>();
    _mockCategoriesClient = new Mock<ICategoriesApiClient>();

    // Register MudBlazor services
    Services.AddMudServices();

    // Register mocked API clients
    Services.AddSingleton(_mockEnvelopesClient.Object);
    Services.AddSingleton(_mockCategoriesClient.Object);
  }

  [Fact]
  public void Component_RendersSuccessfully()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);

    // Assert
    cut.Should().NotBeNull();
    cut.FindComponent<MudAutocomplete<EnvelopeIdName>>().Should().NotBeNull();
  }

  [Fact]
  public void Component_DisplaysPlaceholder()
  {
    // Arrange
    SetupMockData();
    var placeholder = "Test Placeholder";

    // Act
    var cut = Render<EnvelopePicker>(parameters => parameters
      .Add(p => p.Placeholder, placeholder));

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Placeholder.Should().Be(placeholder);
  }

  [Fact]
  public void Component_UsesDefaultPlaceholder_WhenNotSpecified()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Placeholder.Should().Be("Search Envelope");
  }

  [Fact]
  public void Component_IsDisabled_WhenDisabledParameterIsTrue()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>(parameters => parameters
      .Add(p => p.Disabled, true));

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Disabled.Should().BeTrue();
  }

  [Fact]
  public void Component_IsEnabled_WhenDisabledParameterIsFalse()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>(parameters => parameters
      .Add(p => p.Disabled, false));

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Disabled.Should().BeFalse();
  }

  [Fact]
  public async Task Component_LoadsEnvelopesOnInitialization()
  {
    // Arrange
    var envelopes = CreateTestEnvelopes();
    var categories = CreateTestCategories();

    _mockEnvelopesClient
      .Setup(x => x.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(envelopes);

    _mockCategoriesClient
      .Setup(x => x.GetCategoriesAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(categories);

    // Act
    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);
    await Task.Delay(100 , Xunit.TestContext.Current.CancellationToken); // Allow async initialization

    // Assert
    _mockEnvelopesClient.Verify(x => x.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>()), Times.Once);
    _mockCategoriesClient.Verify(x => x.GetCategoriesAsync(It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public void Component_UsesOutlinedVariant_ByDefault()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Variant.Should().Be(Variant.Outlined);
  }

  [Fact]
  public void Component_UsesCustomVariant_WhenSpecified()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>(parameters => parameters
      .Add(p => p.Variant, Variant.Filled));

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Variant.Should().Be(Variant.Filled);
  }

  [Fact]
  public void Component_IsDense_ByDefault()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Dense.Should().BeTrue();
  }

  [Fact]
  public void Component_AppliesCustomStyle_WhenSpecified()
  {
    // Arrange
    SetupMockData();
    var customStyle = "width: 200px;";

    // Act
    var cut = Render<EnvelopePicker>(parameters => parameters
      .Add(p => p.Style, customStyle));

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Style.Should().Be(customStyle);
  }

  [Fact]
  public void Component_AppliesCustomClass_WhenSpecified()
  {
    // Arrange
    SetupMockData();
    var customClass = "custom-picker";

    // Act
    var cut = Render<EnvelopePicker>(parameters => parameters
      .Add(p => p.Class, customClass));

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Class.Should().Be(customClass);
  }

  [Fact]
  public void Component_AcceptsNullValue()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>(parameters => parameters
      .Add(p => p.Value, (EnvelopeIdName?)null));

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance. GetState(x=> x.Value.Should().BeNull());
  }

  [Fact]
  public void Component_AcceptsNonNullValue()
  {
    // Arrange
    SetupMockData();
    var envelope = new EnvelopeIdName(1, "Category1", "Envelope1", 1, 1);

    // Act
    var cut = Render<EnvelopePicker>(parameters => parameters
      .Add(p => p.Value, envelope));

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.GetState(x=> x.Value.Should().Be(envelope));
  }

  [Fact]
  public void Component_UsesMarginNone_ByDefault()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Margin.Should().Be(MudBlazor.Margin.None);
  }

  [Fact]
  public void Component_UsesCustomMargin_WhenSpecified()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>(parameters => parameters
      .Add(p => p.Margin, MudBlazor.Margin.Dense));

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.Margin.Should().Be(MudBlazor.Margin.Dense);
  }

  [Fact]
  public async Task SearchEnvelopes_ReturnsAllEnvelopes_WhenSearchTextIsEmpty()
  {
    // Arrange
    var envelopes = CreateTestEnvelopes();
    var categories = CreateTestCategories();

    _mockEnvelopesClient
      .Setup(x => x.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(envelopes);

    _mockCategoriesClient
      .Setup(x => x.GetCategoriesAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(categories);

    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);
    await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);

    // Act
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    var searchResults = await autocomplete.Instance.SearchFunc!("", CancellationToken.None);

    // Assert
    searchResults.Should().NotBeNull();
    searchResults!.Should().HaveCountGreaterThan(0);
  }

  [Fact]
  public async Task SearchEnvelopes_FiltersEnvelopesByName_WhenSearchTextProvided()
  {
    // Arrange
    var envelopes = new List<EnvelopeDto>
    {
      new() { Id = 1, Name = "Groceries", CategoryId = "1", EnvelopeType = EnvelopeTypes.Standard },
      new() { Id = 2, Name = "Gas", CategoryId = "1", EnvelopeType = EnvelopeTypes.Standard },
      new() { Id = 3, Name = "Salary", CategoryId = "2", EnvelopeType = EnvelopeTypes.Income }
    };

    var categories = CreateTestCategories();

    _mockEnvelopesClient
      .Setup(x => x.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(envelopes);

    _mockCategoriesClient
      .Setup(x => x.GetCategoriesAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(categories);

    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);
    await Task.Delay(100);

    // Act
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    var searchResults = await autocomplete.Instance.SearchFunc!("Groc", CancellationToken.None);

    // Assert
    searchResults.Should().NotBeNull();
    var resultsList = searchResults!.ToList();
    resultsList.Should().ContainSingle();
    resultsList.First().EnvelopeName.Should().Be("Groceries");
  }

  [Fact]
  public async Task SearchEnvelopes_IsCaseInsensitive()
  {
    // Arrange
    var envelopes = new List<EnvelopeDto>
    {
      new() { Id = 1, Name = "Groceries", CategoryId = "1", EnvelopeType = EnvelopeTypes.Standard }
    };

    var categories = CreateTestCategories();

    _mockEnvelopesClient
      .Setup(x => x.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(envelopes);

    _mockCategoriesClient
      .Setup(x => x.GetCategoriesAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(categories);

    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);
    await Task.Delay(100);

    // Act
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    var searchResults = await autocomplete.Instance.SearchFunc!("GROC", CancellationToken.None);

    // Assert
    searchResults.Should().NotBeNull();
    searchResults!.Should().ContainSingle();
  }

  [Fact]
  public void Component_DisplaysSearchIcon()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.AdornmentIcon.Should().Be(Icons.Material.Filled.Search);
  }

  [Fact]
  public void Component_DisplaysProgressIndicator()
  {
    // Arrange
    SetupMockData();

    // Act
    var cut = Render<EnvelopePicker>((Action<ComponentParameterCollectionBuilder<EnvelopePicker>>?)null);

    // Assert
    var autocomplete = cut.FindComponent<MudAutocomplete<EnvelopeIdName>>();
    autocomplete.Instance.ShowProgressIndicator.Should().BeTrue();
  }

  // Helper methods
  private void SetupMockData()
  {
    var envelopes = CreateTestEnvelopes();
    var categories = CreateTestCategories();

    _mockEnvelopesClient
      .Setup(x => x.GetEnvelopesAsync(It.IsAny<EnvelopeTypes>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(envelopes);

    _mockCategoriesClient
      .Setup(x => x.GetCategoriesAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(categories);
  }

  private static List<EnvelopeDto> CreateTestEnvelopes()
  {
    return
    [
      new() { Id = 1, Name = "Groceries", CategoryId = "1", EnvelopeType = EnvelopeTypes.Standard, SortOrder = 1 },
      new() { Id = 2, Name = "Gas", CategoryId = "1", EnvelopeType = EnvelopeTypes.Standard, SortOrder = 2 },
      new() { Id = 3, Name = "Salary", CategoryId = "2", EnvelopeType = EnvelopeTypes.Income, SortOrder = 1 }
    ];
  }

  private static List<CategoryDto> CreateTestCategories()
  {
    return
    [
      new() { CategoryId = "1", Name = "Category1", SortOrder = 1 },
      new() { CategoryId = "2", Name = "Category2", SortOrder = 2 }
    ];
  }
}
