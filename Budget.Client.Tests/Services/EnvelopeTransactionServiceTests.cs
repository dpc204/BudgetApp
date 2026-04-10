using Budget.Client.Services;
using TestContext = Xunit.TestContext;

namespace Budget.Client.Tests.Services;

/// <summary>
/// Tests for EnvelopeTransactionService
/// </summary>
public class EnvelopeTransactionServiceTests
{
  private readonly Mock<ITransactionsApiClient> _mockApi;
  private readonly Mock<IDialogService> _mockDialogService;
  private readonly Mock<IUserAndOptions> _mockUserOptions;
  private readonly Mock<ILogger<EnvelopeTransactionService>> _mockLogger;
  private readonly EnvelopeTransactionService _service;

  public EnvelopeTransactionServiceTests()
  {
    _mockApi = new Mock<ITransactionsApiClient>();
    _mockDialogService = new Mock<IDialogService>();
    _mockUserOptions = new Mock<IUserAndOptions>();
    _mockLogger = new Mock<ILogger<EnvelopeTransactionService>>();
    _service = new EnvelopeTransactionService(
      _mockApi.Object,
      _mockDialogService.Object,
      _mockUserOptions.Object,
      _mockLogger.Object);
  }

  [Fact]
  public async Task LoadTransactionsAsync_ReturnsTransactions()
  {
    // Arrange
    var envelopeId = 1;
    var transactions = new List<TransactionDto>
    {
      new() { TransactionId = 1, Vendor = "Store A", Amount = 50m },
      new() { TransactionId = 2, Vendor = "Store B", Amount = 75m }
    };

    _mockApi
      .Setup(a => a.GetTransactionsByEnvelopeAsync(envelopeId, 0, 0, It.IsAny<CancellationToken>()))
      .ReturnsAsync(transactions);

    // Act
    var result = await _service.LoadTransactionsAsync(envelopeId, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    result.Should().HaveCount(2);
    result[0].Vendor.Should().Be("Store A");
    result[1].Vendor.Should().Be("Store B");
  }

  [Fact]
  public async Task LoadTransactionsAsync_OnException_ReturnsEmptyList()
  {
    // Arrange
    var envelopeId = 1;
    _mockApi
      .Setup(a => a.GetTransactionsByEnvelopeAsync(envelopeId, 0, 0, It.IsAny<CancellationToken>()))
      .ThrowsAsync(new Exception("API error"));

    // Act
    var result = await _service.LoadTransactionsAsync(envelopeId, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public async Task ShowTransactionDetailsAsync_AsAdmin_WithEdit_ReturnsUpdatedEnvelopes()
  {
    // Arrange
    var transactionId = 1;
    var transactionDetail = new OneTransactionDetail {
      Id = transactionId,
      Vendor = "Store A",
      TotalAmount = 50m
    };

    var updatedEnvelopes = new EnvelopeDeltas
    {
      new( 1, 150m )
    };

    _mockApi
      .Setup(a => a.GetOneTransactionDetailAsync(transactionId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(transactionDetail);

    _mockUserOptions.Setup(u => u.IsAdminUser()).Returns(true);

    var mockDialogReference = new Mock<IDialogReference>();
    var dialogResult = DialogResult.Ok(updatedEnvelopes);
    mockDialogReference.Setup(d => d.Result).ReturnsAsync(dialogResult);

    _mockDialogService
      .Setup(d => d.ShowAsync<EditTransactionDialog>(
        It.IsAny<string>(),
        It.IsAny<DialogParameters>(),
        It.IsAny<DialogOptions>()))
      .ReturnsAsync(mockDialogReference.Object);

    // Act
    var result = await _service.ShowTransactionDetailsAsync(transactionId, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    result.Should().NotBeNull();
    result!.WasEdited.Should().BeTrue();
    result.Deltas.Should().HaveCount(1);
    result.Deltas[0].EnvelopeId.Should().Be(1);
  }

  [Fact]
  public async Task ShowTransactionDetailsAsync_AsAdmin_Canceled_ReturnsNull()
  {
    // Arrange
    var transactionId = 1;
    var transactionDetail = new OneTransactionDetail {
      Id = transactionId,
      Vendor = "Store A",
      TotalAmount = 50m
    };

    _mockApi
      .Setup(a => a.GetOneTransactionDetailAsync(transactionId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(transactionDetail);

    _mockUserOptions.Setup(u => u.IsAdminUser()).Returns(true);

    var mockDialogReference = new Mock<IDialogReference>();
    var dialogResult = DialogResult.Cancel();
    mockDialogReference.Setup(d => d.Result).ReturnsAsync(dialogResult);

    _mockDialogService
      .Setup(d => d.ShowAsync<EditTransactionDialog>(
        It.IsAny<string>(),
        It.IsAny<DialogParameters>(),
        It.IsAny<DialogOptions>()))
      .ReturnsAsync(mockDialogReference.Object);

    // Act
    var result = await _service.ShowTransactionDetailsAsync(transactionId, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task ShowTransactionDetailsAsync_AsNonAdmin_ShowsReadOnlyDialog()
  {
    // Arrange
    var transactionId = 1;
    var transactionDetail = new OneTransactionDetail {
      Id = transactionId,
      Vendor = "Store A",
      TotalAmount = 50m
    };

    _mockApi
      .Setup(a => a.GetOneTransactionDetailAsync(transactionId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(transactionDetail);

    _mockUserOptions.Setup(u => u.IsAdminUser()).Returns(false);

    var mockDialogReference = new Mock<IDialogReference>();
    var dialogResult = DialogResult.Ok<object?>(null);
    mockDialogReference.Setup(d => d.Result).ReturnsAsync(dialogResult);

    _mockDialogService
      .Setup(d => d.ShowAsync<ShowOneTransaction>(
        It.IsAny<string>(),
        It.IsAny<DialogParameters>(),
        It.IsAny<DialogOptions>()))
      .ReturnsAsync(mockDialogReference.Object);

    // Act
    var result = await _service.ShowTransactionDetailsAsync(transactionId, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    result.Should().BeNull();
    _mockDialogService.Verify(d => d.ShowAsync<ShowOneTransaction>(
      "Transaction Details",
      It.IsAny<DialogParameters>(),
      It.IsAny<DialogOptions>()), Times.Once);
  }

  [Fact]
  public async Task ShowTransactionDetailsAsync_OnException_ReturnsNull()
  {
    // Arrange
    var transactionId = 1;
    _mockApi
      .Setup(a => a.GetOneTransactionDetailAsync(transactionId, It.IsAny<CancellationToken>()))
      .ThrowsAsync(new Exception("API error"));

    // Act
    var result = await _service.ShowTransactionDetailsAsync(transactionId, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task ShowNewTransactionDialogAsync_WithSave_ReturnsUpdatedEnvelopes()
  {
    // Arrange
    var envelopeId = 1;
    var updatedEnvelopes = new EnvelopeDeltas
    {
      new( envelopeId, 150m )
    };

    var mockDialogReference = new Mock<IDialogReference>();
    var dialogResult = DialogResult.Ok(updatedEnvelopes);
    mockDialogReference.Setup(d => d.Result).ReturnsAsync(dialogResult);

    _mockDialogService
      .Setup(d => d.ShowAsync<EditTransactionDialog>(
        It.IsAny<string>(),
        It.IsAny<DialogParameters>(),
        It.IsAny<DialogOptions>()))
      .ReturnsAsync(mockDialogReference.Object);

    // Act
    var result = await _service.ShowNewTransactionDialogAsync(envelopeId);

    // Assert
    result.Should().NotBeNull();
    result!.WasEdited.Should().BeTrue();
    result.Deltas.Should().HaveCount(1);
    result.Deltas[0].EnvelopeId.Should().Be(envelopeId);
  }

  [Fact]
  public async Task ShowNewTransactionDialogAsync_Canceled_ReturnsNull()
  {
    // Arrange
    var envelopeId = 1;
    var mockDialogReference = new Mock<IDialogReference>();
    var dialogResult = DialogResult.Cancel();
    mockDialogReference.Setup(d => d.Result).ReturnsAsync(dialogResult);

    _mockDialogService
      .Setup(d => d.ShowAsync<EditTransactionDialog>(
        It.IsAny<string>(),
        It.IsAny<DialogParameters>(),
        It.IsAny<DialogOptions>()))
      .ReturnsAsync(mockDialogReference.Object);

    // Act
    var result = await _service.ShowNewTransactionDialogAsync(envelopeId);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task ShowNewTransactionDialogAsync_OnException_ReturnsNull()
  {
    // Arrange
    var envelopeId = 1;
    _mockDialogService
      .Setup(d => d.ShowAsync<EditTransactionDialog>(
        It.IsAny<string>(),
        It.IsAny<DialogParameters>(),
        It.IsAny<DialogOptions>()))
      .ThrowsAsync(new Exception("Dialog error"));

    // Act
    var result = await _service.ShowNewTransactionDialogAsync(envelopeId);

    // Assert
    result.Should().BeNull();
  }
}
