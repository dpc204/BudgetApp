using Budget.Client.Services;

namespace Budget.Client.Tests.Services;

/// <summary>
/// Tests for FundDataService
/// </summary>
public class FundDataServiceTests
{
  private readonly Mock<IBudgetMonthlyApiClient> _mockApiClient;
  private readonly Mock<ILogger<FundDataService>> _mockLogger;
  private readonly FundDataService _service;

  public FundDataServiceTests()
  {
    _mockApiClient = new Mock<IBudgetMonthlyApiClient>();
    _mockLogger = new Mock<ILogger<FundDataService>>();
    _service = new FundDataService(_mockApiClient.Object, _mockLogger.Object);
  }

  [Fact]
  public async Task LoadFundDataAsync_ReturnsCorrectData()
  {
    // Arrange
    var year = 2026;
    var month = 1;
    var acctPeriod = year * 100 + month;

    var monthData = new List<BudgetMonthResponse>
    {
      new(
        AcctPeriod: acctPeriod,
        EnvelopeId: 1,
        EnvelopeName: "Groceries",
        CategoryId: "1",
        CategoryName: "Food",
        CategoryType: CatTypes.User,
        SortOrder: 1,
        Budget: 500m,
        BudgetDraft: null,
        IsBudgetLocked: false,
        FundAmount: 100m,
        Balance: 200m
      ),
      new(
        AcctPeriod: acctPeriod,
        EnvelopeId: 2,
        EnvelopeName: "Gas",
        CategoryId: "2",
        CategoryName: "Transportation",
        CategoryType: CatTypes.User,
        SortOrder: 2,
        Budget: 300m,
        BudgetDraft: null,
        IsBudgetLocked: false,
        FundAmount: 50m,
        Balance: 100m
      ),
      new(
        AcctPeriod: acctPeriod,
        EnvelopeId: 3,
        EnvelopeName: "Income",
        CategoryId: "3",
        CategoryName: "Salary",
        CategoryType: CatTypes.Income,
        SortOrder: 1,
        Budget: 5000m,
        BudgetDraft: null,
        IsBudgetLocked: false,
        FundAmount: 0m,
        Balance: 5000m
      )
    };

    var unallocatedEnvelope = new EnvelopeDto
    {
      Id = 100,
      Name = "Unallocated",
      Balance = 1000m
    };

    _mockApiClient
      .Setup(x => x.GetBudgetMonthAsync(year, month, It.IsAny<CancellationToken>()))
      .ReturnsAsync(monthData);

    _mockApiClient
      .Setup(x => x.GetEnvelopeByEnvelopeTypeAsync(EnvelopeTypes.Income, It.IsAny<CancellationToken>()))
      .ReturnsAsync(unallocatedEnvelope);

    // Act
    var result = await _service.LoadFundDataAsync(year, month);

    // Assert
    result.Should().NotBeNull();
    result.FundData.Should().HaveCount(2); // Only User category envelopes
    result.TotalBudget.Should().Be(800m); // 500 + 300
    result.TotalBalance.Should().Be(300m); // 200 + 100
    result.AvailableToFund.Should().Be(850m); // 1000 - 100 - 50
  }

  [Fact]
  public async Task LoadFundDataAsync_FiltersNonUserEnvelopes()
  {
    // Arrange
    var year = 2026;
    var month = 1;
    var acctPeriod = year * 100 + month;

    var monthData = new List<BudgetMonthResponse>
    {
      new(
        AcctPeriod: acctPeriod,
        EnvelopeId: 1,
        EnvelopeName: "Groceries",
        CategoryId: "1",
        CategoryName: "Food",
        CategoryType: CatTypes.User,
        SortOrder: 1,
        Budget: 500m,
        BudgetDraft: null,
        IsBudgetLocked: false,
        FundAmount: 0m,
        Balance: 0m
      ),
      new(
        AcctPeriod: acctPeriod,
        EnvelopeId: 2,
        EnvelopeName: "Salary",
        CategoryId: "2",
        CategoryName: "Income",
        CategoryType: CatTypes.Income,
        SortOrder: 1,
        Budget: 5000m,
        BudgetDraft: null,
        IsBudgetLocked: false,
        FundAmount: 0m,
        Balance: 5000m
      )
    };

    var unallocatedEnvelope = new EnvelopeDto
    {
      Id = 100,
      Name = "Unallocated",
      Balance = 1000m
    };

    _mockApiClient
      .Setup(x => x.GetBudgetMonthAsync(year, month, It.IsAny<CancellationToken>()))
      .ReturnsAsync(monthData);

    _mockApiClient
      .Setup(x => x.GetEnvelopeByEnvelopeTypeAsync(EnvelopeTypes.Income, It.IsAny<CancellationToken>()))
      .ReturnsAsync(unallocatedEnvelope);

    // Act
    var result = await _service.LoadFundDataAsync(year, month);

    // Assert
    result.FundData.Should().ContainSingle(); // Only the User category envelope
    result.FundData.Should().ContainKey(1);
    result.FundData.Should().NotContainKey(2); // Income envelope should be filtered out
  }

  [Fact]
  public void BuildDisplayRows_SortsEnvelopesBySortOrder()
  {
    // Arrange
    var fundData = new Dictionary<int, FundEnvelopeData>();
    fundData[1] = new FundEnvelopeData
    {
      EnvelopeId = 1,
      EnvelopeName = "Gas",
      SortOrder = 3,
      Budget = 300m,
      CurrentBalance = 100m,
      FundAmount = 50m
    };
    fundData[2] = new FundEnvelopeData
    {
      EnvelopeId = 2,
      EnvelopeName = "Groceries",
      SortOrder = 1,
      Budget = 500m,
      CurrentBalance = 200m,
      FundAmount = 100m
    };
    fundData[3] = new FundEnvelopeData
    {
      EnvelopeId = 3,
      EnvelopeName = "Utilities",
      SortOrder = 2,
      Budget = 200m,
      CurrentBalance = 50m,
      FundAmount = 75m
    };

    // Act
    var result = _service.BuildDisplayRows(fundData);

    // Assert
    result.Should().HaveCount(3);
    result[0].EnvelopeName.Should().Be("Groceries"); // SortOrder 1
    result[1].EnvelopeName.Should().Be("Utilities"); // SortOrder 2
    result[2].EnvelopeName.Should().Be("Gas"); // SortOrder 3
  }

  [Fact]
  public void BuildDisplayRows_WithEmptyData_ReturnsEmptyList()
  {
    // Arrange
    var fundData = new Dictionary<int, FundEnvelopeData>();

    // Act
    var result = _service.BuildDisplayRows(fundData);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public void BuildDisplayRows_WithNullData_ReturnsEmptyList()
  {
    // Arrange
    Dictionary<int, FundEnvelopeData>? fundData = null;

    // Act
    var result = _service.BuildDisplayRows(fundData!);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public void BuildDisplayRows_MapsAllPropertiesCorrectly()
  {
    // Arrange
    var fundData = new Dictionary<int, FundEnvelopeData>();
    fundData[1] = new FundEnvelopeData
    {
      EnvelopeId = 1,
      EnvelopeName = "Groceries",
      SortOrder = 1,
      Budget = 500m,
      CurrentBalance = 200m,
      FundAmount = 100m
    };

    // Act
    var result = _service.BuildDisplayRows(fundData);

    // Assert
    result.Should().ContainSingle();
    var row = result[0];
    row.EnvelopeId.Should().Be(1);
    row.EnvelopeName.Should().Be("Groceries");
    row.CurrentBalance.Should().Be(200m);
    row.Budget.Should().Be(500m);
    row.FundAmount.Should().Be(100m);
    row.UpdateCounter.Should().Be(0);
  }
}
