using Microsoft.AspNetCore.Components;
using Budget.Client.Pages;

namespace Budget.Client.Tests.Pages;

/// <summary>
/// Tests for the Budget page Tab/Enter navigation functionality
/// </summary>
public class BudgetPageNavigationTests : TestContext
{
  private readonly Mock<IBudgetMonthlyApiClient> _mockBudgetApi;
  private readonly Mock<IDialogService> _mockDialogService;
  private readonly Mock<ISnackbar> _mockSnackbar;
  private readonly Mock<IJSRuntime> _mockJsRuntime;

  public BudgetPageNavigationTests()
  {
    _mockBudgetApi = new Mock<IBudgetMonthlyApiClient>();
    _mockDialogService = new Mock<IDialogService>();
    _mockSnackbar = new Mock<ISnackbar>();
    _mockJsRuntime = new Mock<IJSRuntime>();

    // Register services
    Services.AddMudServices();
    Services.AddSingleton(_mockBudgetApi.Object);
    Services.AddSingleton(_mockDialogService.Object);
    Services.AddSingleton(_mockSnackbar.Object);
    Services.AddSingleton(_mockJsRuntime.Object);
    
    // Add MudBlazor providers to the component tree
    ComponentFactories.AddStub<MudPopoverProvider>();
    ComponentFactories.AddStub<MudDialogProvider>();
    ComponentFactories.AddStub<MudSnackbarProvider>();
  }

  [Fact]
  public void Budget_Page_Renders_Successfully()
  {
    // Arrange
    SetupMockApiResponses();

    // Act
    var cut = RenderComponent<Budget.Client.Pages.Budget>();

    // Assert
    Assert.NotNull(cut);
    cut.WaitForAssertion(() => Assert.Contains("Budget Maintenance", cut.Markup), timeout: TimeSpan.FromSeconds(10));
  }

  [Fact]
  public void Budget_Page_Initializes_JavaScript_Navigation_OnFirstRender()
  {
    // Arrange
    SetupMockApiResponses();

    // Act
    var cut = RenderComponent<Budget.Client.Pages.Budget>();

    // Wait a bit for the component to finish loading
    System.Threading.Thread.Sleep(2000);

    // Assert - verify that JS was called to initialize navigation
    _mockJsRuntime.Verify(js => js.InvokeAsync<object>(
      "initializeDraftFieldNavigation",
      It.IsAny<object[]>()), Times.AtLeastOnce);
  }

  [Fact]
  public void Budget_Page_Renders_DraftFields_With_Data_Attributes()
  {
    // Arrange
    SetupMockApiResponses();

    // Act
    var cut = RenderComponent<Budget.Client.Pages.Budget>();
    
    // Wait for component to render
    System.Threading.Thread.Sleep(2000);

    // Assert - Check that draft fields have the necessary data attributes
    var markup = cut.Markup;
    
    // The UserAttributes should render on the MudNumericField component
    // We should see data-envelope-id and data-month-index attributes
    Assert.Contains("data-envelope-id", markup);
    Assert.Contains("data-month-index", markup);
  }

  [Fact]
  public void Budget_Page_DraftFields_Have_Draft_Prefix_IDs()
  {
    // Arrange
    SetupMockApiResponses();

    // Act
    var cut = RenderComponent<Budget.Client.Pages.Budget>();
    
    // Wait for component to render
    System.Threading.Thread.Sleep(2000);

    // Assert
    var markup = cut.Markup;
    
    // Check that draft field IDs follow the pattern draft-{envelopeId}-{monthIndex}
    // This pattern should be present in the rendered HTML
    Assert.Contains("draft-", markup);
  }

  private void SetupMockApiResponses()
  {
    var currentDate = DateTime.Now;
    var acctPeriod = currentDate.Year * 100 + currentDate.Month;

    // Setup CheckDraftBudgetsAsync
    _mockBudgetApi
      .Setup(x => x.CheckDraftBudgetsAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(new CheckDraftsResponse(false, 0));

    // Setup GetBudgetMonthAsync to return test data
    var testData = new List<BudgetMonthResponse>
    {
      new(
        AcctPeriod: acctPeriod,
        EnvelopeId: 1,
        EnvelopeName: "Groceries",
        CategoryId: 1,
        CategoryName: "Food",
        CategoryType: CatTypes.User,
        SortOrder: 1,
        Budget: 500.00m,
        BudgetDraft: null,
        IsBudgetLocked: false
      ),
      new(
        AcctPeriod: acctPeriod,
        EnvelopeId: 2,
        EnvelopeName: "Gas",
        CategoryId: 2,
        CategoryName: "Transportation",
        CategoryType: CatTypes.User,
        SortOrder: 2,
        Budget: 200.00m,
        BudgetDraft: null,
        IsBudgetLocked: false
      ),
      new(
        AcctPeriod: acctPeriod,
        EnvelopeId: 3,
        EnvelopeName: "Salary",
        CategoryId: 3,
        CategoryName: "Income",
        CategoryType: CatTypes.Income,
        SortOrder: 1,
        Budget: 5000.00m,
        BudgetDraft: null,
        IsBudgetLocked: false
      )
    };

    _mockBudgetApi
      .Setup(x => x.GetBudgetMonthAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(testData);

    // Setup JSRuntime for screen size check
    _mockJsRuntime
      .Setup(x => x.InvokeAsync<int>("windowUtils.getInnerWidth", It.IsAny<object[]>()))
      .ReturnsAsync(1920);
  }
}
