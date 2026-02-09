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
            var envelope = new Budget.DB.Envelope
            {
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
        [InlineData(EnvelopeTypes.Standard)]
        [InlineData(EnvelopeTypes.Income)]
        [InlineData(EnvelopeTypes.Unassigned)]
        [InlineData(EnvelopeTypes.All)]
        public async Task Get_AllEnvelopeTypes_ReturnsCorrectEnvelope(EnvelopeTypes envelopeType)
        {
            // Arrange
            await using var context = new BudgetContext(CreateInMemoryOptions(), null);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var envelope = new Budget.DB.Envelope
            {
                Id = (int)envelopeType,
                Name = $"{envelopeType} Envelope",
                CategoryId = "1",
                EnvelopeType = envelopeType,
                FamilyId = 1
            };
            context.Envelopes.Add(envelope);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken );
            // Act
            var result = await GetEnvelopeByType.Get(context, envelopeType, CancellationToken.None);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(envelopeType, result.EnvelopeType);
            Assert.Equal($"{envelopeType} Envelope", result.Name);
        }

        /// <summary>
        /// Tests that Get returns null when an invalid (out-of-range) EnvelopeType value is provided.
        /// Input: Invalid EnvelopeType value (999).
        /// Expected: Returns null without throwing an exception.
        /// </summary>
        [Trait("Category", "ProductionBugSuspected")]
        [Fact]
        public async Task Get_InvalidEnvelopeType_ReturnsNull()
        {
            // Arrange
            await using var context = new BudgetContext(CreateInMemoryOptions(), null);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var envelope = new Budget.DB.Envelope
            {
                Id = 1,
                Name = "Test Envelope",
                CategoryId = "1",
                EnvelopeType = EnvelopeTypes.Standard,
                FamilyId = 1
            };
            context.Envelopes.Add(envelope);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var invalidEnvelopeType = (EnvelopeTypes)999;
            // Act
            var result = await GetEnvelopeByType.Get(context, invalidEnvelopeType, CancellationToken.None);
            // Assert
            Assert.Null(result);
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
            var envelope = new Budget.DB.Envelope
            {
                Id = 100,
                Name = "Test Envelope",
                CategoryId = "1",
                EnvelopeType = EnvelopeTypes.Standard,
                FamilyId = 1
            };
            context.Envelopes.Add(envelope);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await GetEnvelopeByType.Get(context, EnvelopeTypes.Standard, cancellationTokenSource.Token));
        }

        /// <summary>
        /// Tests that Get throws an exception when a null database context is provided.
        /// Input: null BudgetContext.
        /// Expected: NullReferenceException or ArgumentNullException.
        /// </summary>
        [Trait("Category", "ProductionBugSuspected")]
        [Fact]
        public async Task Get_NullDatabase_ThrowsException()
        {
            // Arrange
            BudgetContext? context = null;
            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(async () => await GetEnvelopeByType.Get(context!, EnvelopeTypes.Standard, CancellationToken.None));
        }

        /// <summary>
        /// Tests that Get returns an envelope with null Budget when Budget is not set.
        /// Input: Envelope with null Budget property.
        /// Expected: EnvelopeDto with null Budget.
        /// </summary>
        [Fact]
        public async Task Get_EnvelopeWithNullBudget_ReturnsEnvelopeWithNullBudget()
        {
            // Arrange
            await using var context = new BudgetContext(CreateInMemoryOptions(), null);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var envelope = new Budget.DB.Envelope
            {
                Id = 100,
                Name = "No Budget Envelope",
                CategoryId = "1",
                EnvelopeType = EnvelopeTypes.Standard,
                Budget = null,
                Balance = 0m,
                FamilyId = 1
            };
            context.Envelopes.Add(envelope);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            // Act
            var result = await GetEnvelopeByType.Get(context, EnvelopeTypes.Standard, CancellationToken.None);
            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Budget);
            Assert.Equal(0m, result.Balance);
        }

    }
}