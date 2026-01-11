using Budget.Client.Services;

namespace Budget.Client.Tests.Services;

/// <summary>
/// Tests for FundAllocationService
/// </summary>
public class FundAllocationServiceTests
{
  private readonly FundAllocationService _service;

  public FundAllocationServiceTests()
  {
    _service = new FundAllocationService();
  }

  [Fact]
  public void CalculateFundAmount_OneHundredPercent_ReturnsFullBudget()
  {
    // Arrange
    decimal budget = 500m;
    decimal currentBalance = 100m;
    var fillType = FillAmounts.OneHundredPercent;

    // Act
    var result = _service.CalculateFundAmount(budget, currentBalance, fillType);

    // Assert
    result.Should().Be(500m);
  }

  [Fact]
  public void CalculateFundAmount_FiftyPercent_ReturnsHalfBudget()
  {
    // Arrange
    decimal budget = 500m;
    decimal currentBalance = 100m;
    var fillType = FillAmounts.FiftyPercent;

    // Act
    var result = _service.CalculateFundAmount(budget, currentBalance, fillType);

    // Assert
    result.Should().Be(250m);
  }

  [Fact]
  public void CalculateFundAmount_FillToBudget_WithBalanceBelowBudget_ReturnsDifference()
  {
    // Arrange
    decimal budget = 500m;
    decimal currentBalance = 200m;
    var fillType = FillAmounts.FillToBudget;

    // Act
    var result = _service.CalculateFundAmount(budget, currentBalance, fillType);

    // Assert
    result.Should().Be(300m);
  }

  [Fact]
  public void CalculateFundAmount_FillToBudget_WithBalanceAtBudget_ReturnsZero()
  {
    // Arrange
    decimal budget = 500m;
    decimal currentBalance = 500m;
    var fillType = FillAmounts.FillToBudget;

    // Act
    var result = _service.CalculateFundAmount(budget, currentBalance, fillType);

    // Assert
    result.Should().Be(0m);
  }

  [Fact]
  public void CalculateFundAmount_FillToBudget_WithBalanceAboveBudget_ReturnsZero()
  {
    // Arrange
    decimal budget = 500m;
    decimal currentBalance = 600m;
    var fillType = FillAmounts.FillToBudget;

    // Act
    var result = _service.CalculateFundAmount(budget, currentBalance, fillType);

    // Assert
    result.Should().Be(0m);
  }

  [Fact]
  public void CalculateFundAmount_NoBudget_ReturnsNull()
  {
    // Arrange
    decimal? budget = null;
    decimal currentBalance = 100m;
    var fillType = FillAmounts.OneHundredPercent;

    // Act
    var result = _service.CalculateFundAmount(budget, currentBalance, fillType);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void CalculateFundAmount_NotSet_ReturnsNull()
  {
    // Arrange
    decimal budget = 500m;
    decimal currentBalance = 100m;
    var fillType = FillAmounts.NotSet;

    // Act
    var result = _service.CalculateFundAmount(budget, currentBalance, fillType);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void CalculateFundAmounts_MultipleEnvelopes_ReturnsCorrectAmounts()
  {
    // Arrange
    var envelopes = new List<TestFundableEnvelope>
    {
      new() { EnvelopeId = 1, Budget = 500m, CurrentBalance = 100m },
      new() { EnvelopeId = 2, Budget = 300m, CurrentBalance = 50m },
      new() { EnvelopeId = 3, Budget = null, CurrentBalance = 0m } // No budget
    };
    var fillType = FillAmounts.OneHundredPercent;

    // Act
    var results = _service.CalculateFundAmounts(envelopes, fillType);

    // Assert
    results.Should().HaveCount(2);
    results[1].Should().Be(500m);
    results[2].Should().Be(300m);
    results.Should().NotContainKey(3); // Envelope with no budget should not be in results
  }

  [Fact]
  public void CalculateFundAmounts_FillToBudget_ReturnsCorrectAmounts()
  {
    // Arrange
    var envelopes = new List<TestFundableEnvelope>
    {
      new() { EnvelopeId = 1, Budget = 500m, CurrentBalance = 200m }, // Needs 300
      new() { EnvelopeId = 2, Budget = 300m, CurrentBalance = 300m }, // Needs 0
      new() { EnvelopeId = 3, Budget = 400m, CurrentBalance = 450m }  // Needs 0 (over budget)
    };
    var fillType = FillAmounts.FillToBudget;

    // Act
    var results = _service.CalculateFundAmounts(envelopes, fillType);

    // Assert
    results.Should().HaveCount(3);
    results[1].Should().Be(300m);
    results[2].Should().Be(0m);
    results[3].Should().Be(0m);
  }

  /// <summary>
  /// Test implementation of IFundableEnvelope
  /// </summary>
  private class TestFundableEnvelope : IFundableEnvelope
  {
    public int EnvelopeId { get; set; }
    public decimal? Budget { get; set; }
    public decimal CurrentBalance { get; set; }
  }
}
