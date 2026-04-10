using Budget.Api.Shared;
using Budget.Shared.Enums;

namespace Budget.ApiTests.Shared
{
  /// <summary>
  /// Unit tests for the GetEnvelopeByType class.
  /// </summary>
  public partial class GetEnvelopeByTypeTests
  {
    /// <summary>
    /// Creates an in-memory database options instance for testing.
    /// </summary>
    /// <returns>DbContextOptions configured for in-memory database.</returns>
    private static DbContextOptions<BudgetContext> CreateInMemoryOptions()
    {
      return new DbContextOptionsBuilder<BudgetContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    }

    /// <summary>
    /// Tests that Get returns null when no envelope matches the specified EnvelopeType.
    /// Input: Valid EnvelopeType with no matching envelope in database.
    /// Expected: Returns null.
    /// </summary>
    [Fact]
    public async Task Get_ValidEnvelopeTypeWithNoMatch_ReturnsNull()
    {
      // Arrange
      await using var context = new BudgetContext(CreateInMemoryOptions(), null);
      await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
      var envelope = new Budget.DB.Envelope {
        Id = 100,
        Name = "Test Envelope",
        CategoryId = "1",
        EnvelopeType = EnvelopeTypes.Standard,
        FamilyId = 1
      };
      context.Envelopes.Add(envelope);
      await context.SaveChangesAsync(TestContext.Current.CancellationToken);
      // Act
      var result = await GetEnvelopeByType.Get(context, EnvelopeTypes.Income, CancellationToken.None);
      // Assert
      Assert.Null(result);
    }

    /// <summary>
    /// Tests that Get returns the correct envelope for each valid EnvelopeTypes enum value.
    /// Input: Valid EnvelopeTypes enum values (Standard, Income, Unassigned, All).
    /// Expected: Returns the matching envelope for each type.
    /// </summary>
    [Theory]
    [InlineData(21, EnvelopeTypes.Standard)]
    [InlineData(23, EnvelopeTypes.All)]
    public async Task Get_NotSpecialEnvelopeType_ThrowsArgumentError(int envelopeId, EnvelopeTypes envelopeType)
    {
      // Arrange
      await using var context = new BudgetContext(CreateInMemoryOptions(), null);
      await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
      var envelope = new Budget.DB.Envelope {
        Id = envelopeId,
        Name = $"{envelopeType} Envelope",
        CategoryId = "1",
        EnvelopeType = envelopeType,
        FamilyId = 1
      };
      context.Envelopes.Add(envelope);
      await context.SaveChangesAsync(TestContext.Current.CancellationToken);
      // Act

      // test that the following line throws an ArgumentException for EnvelopeTypes.Income and EnvelopeTypes.Unassigned, but not for EnvelopeTypes.Standard
      await Assert.ThrowsAsync<ArgumentException>(() =>
        GetEnvelopeByType.Get(context, envelopeType, CancellationToken.None));
    }

    [Theory]
    [InlineData(22, EnvelopeTypes.Income)]
    [InlineData(23, EnvelopeTypes.Unassigned)]
    public async Task Get_AllEnvelopeTypes_ReturnsCorrectEnvelope(int envelopeId, EnvelopeTypes envelopeType)
    {
      // Arrange
      await using var context = new BudgetContext(CreateInMemoryOptions(), null);
      await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
      var envelope = new Budget.DB.Envelope {
        Id = envelopeId,
        Name = $"{envelopeType} Envelope",
        CategoryId = "1",
        EnvelopeType = envelopeType,
        FamilyId = 1
      };
      context.Envelopes.Add(envelope);
      await context.SaveChangesAsync(TestContext.Current.CancellationToken);
      // Act
      var result = await GetEnvelopeByType.Get(context, envelopeType, CancellationToken.None);
      // Assert
      Assert.NotNull(result);
      Assert.Equal(envelopeType, result.EnvelopeType);
      Assert.Equal($"{envelopeType} Envelope", result.Name);
    }



    /// <summary>
    /// Tests that Get throws OperationCanceledException when a cancelled CancellationToken is provided.
    /// Input: Already cancelled CancellationToken.
    /// Expected: OperationCanceledException is thrown.
    /// </summary>
    [Fact]
    public async Task Get_CancelledToken_ThrowsOperationCanceledException()
    {
      // Arrange
      await using var context = new BudgetContext(CreateInMemoryOptions(), null);
      await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
      var envelope = new Budget.DB.Envelope {
        Id = 100,
        Name = "Test Envelope",
        CategoryId = "1",
        EnvelopeType = EnvelopeTypes.Unassigned,
        FamilyId = 1
      };
      context.Envelopes.Add(envelope);
      await context.SaveChangesAsync(TestContext.Current.CancellationToken);
      var cancellationTokenSource = new CancellationTokenSource();
      cancellationTokenSource.Cancel();
      // Act & Assert
      await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        await GetEnvelopeByType.Get(context, EnvelopeTypes.Unassigned, cancellationTokenSource.Token));
    }



  }
}