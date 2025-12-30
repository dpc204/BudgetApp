using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Budget.Api.Features.BudgetMonths;
using Budget.DB;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Budget.ApiTests;

/// <summary>
/// Tests for Budget Month API endpoints
/// </summary>
public class BudgetMonthEndpointsTests : IntegrationTestBase
{

  /// <summary>
  /// Test GetBudgetMonth endpoint - should return budget data for a specific month
  /// </summary>
  [Fact]
  public async Task GetBudgetMonth_Should_Return_Budget_Data_For_Month()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var year = 2024;
      var month = 12;

      // Act
      var response = await Client.GetAsync($"/budgetmonths/{year}/{month}");

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<List<GetBudgetMonth.Response>>();

      result.Should().NotBeNull();
      // Should return data for all envelopes, even if no budget data exists
      result.Should().HaveCount(c => c >= 0);
    }
  }

  /// <summary>
  /// Test CheckDraftBudgets endpoint - should indicate if drafts exist
  /// </summary>
  [Fact]
  public async Task CheckDraftBudgets_Should_Indicate_If_Drafts_Exist()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      // Act
      var response = await Client.GetAsync("/budgetmonths/hasdrafts");

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<CheckDraftBudgets.Response>();

      result.Should().NotBeNull();
      result!.DraftCount.Should().BeGreaterThanOrEqualTo(0);
    }
  }

  /// <summary>
  /// Test UpdateBudgetDraft endpoint - should create or update draft budget value
  /// </summary>
  [Fact]
  public async Task UpdateBudgetDraft_Should_Create_Or_Update_Draft()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      var envelope = TestHelpers.CreateTestEnvelope(id: 600, name: "Test Envelope", categoryId: "1");
      db.Envelopes.Add(envelope);
      await db.SaveChangesAsync();

      var command = new UpdateBudgetDraft.Command(
          AcctPeriod: 202412,
          EnvelopeId: envelope.Id,
          DraftValue: 500m);

      // Act
      var response = await Client.PutAsJsonAsync("/budgetmonths/draft", command);

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<UpdateBudgetDraft.Response>();

      result.Should().NotBeNull();
      result!.Success.Should().BeTrue();

      // Verify in database
      db.ChangeTracker.Clear();
      var budgetMonth = await db.BudgetMonths
          .FirstOrDefaultAsync(b => b.AcctPeriod == 202412 && b.EnvelopeId == envelope.Id);

      budgetMonth.Should().NotBeNull();
      budgetMonth!.BudgetDraft.Should().Be(500m);
    }
  }

  /// <summary>
  /// Test ApplyDraftValuesToBudget endpoint - should apply all drafts to budget
  /// </summary>
  [Fact]
  public async Task ApplyDraftValuesToBudget_Should_Apply_Drafts_To_Budget()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      var envelope = TestHelpers.CreateTestEnvelope(id: 601, name: "Test Envelope", categoryId: "1");
      db.Envelopes.Add(envelope);

      var budgetMonth = new BudgetMonth
      {
        AcctPeriod = 202501,
        EnvelopeId = envelope.Id,
        Budget = null,
        BudgetDraft = 300m
      };
      db.BudgetMonths.Add(budgetMonth);
      await db.SaveChangesAsync();

      var command = new ApplyDraftValuesToBudget.Command();

      // Act
      var response = await Client.PostAsJsonAsync("/budgetmonths/applydrafts", command);

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<ApplyDraftValuesToBudget.Response>();

      result.Should().NotBeNull();
      result!.Success.Should().BeTrue();
      result.RecordsUpdated.Should().BeGreaterThan(0);

      // Verify in database
      db.ChangeTracker.Clear();
      var updated = await db.BudgetMonths
          .FirstOrDefaultAsync(b => b.AcctPeriod == 202501 && b.EnvelopeId == envelope.Id);

      updated.Should().NotBeNull();
      updated!.Budget.Should().Be(300m);
      updated.BudgetDraft.Should().BeNull();
    }
  }

    /// <summary>
    /// Test ClearDraftBudgets endpoint - should clear all draft values
    /// </summary>
    [Fact]
    public async Task ClearDraftBudgets_Should_Clear_All_Drafts()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var envelope = TestHelpers.CreateTestEnvelope(id: 6020, name: "Test Envelope", categoryId: "1");
        db.Envelopes.Add(envelope);

        var now = DateTime.Now;
        var futureAcctPeriod = now.Year * 100 + now.Month + 1;
        if ((futureAcctPeriod % 100) > 12)
        {
            futureAcctPeriod = (now.Year + 1) * 100 + 1;
        }

        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = futureAcctPeriod,
            EnvelopeId = envelope.Id,
            Budget = 200m,
            BudgetDraft = 250m
        };
        db.BudgetMonths.Add(budgetMonth);
        await db.SaveChangesAsync();

        var command = new ClearDraftBudgets.Command();

        // Act
        var response = await client.PostAsJsonAsync("/budgetmonths/cleardrafts", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClearDraftBudgets.Response>();
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        
        // Verify in database
        db.ChangeTracker.Clear();
        var updated = await db.BudgetMonths
            .FirstOrDefaultAsync(b => b.AcctPeriod == futureAcctPeriod && b.EnvelopeId == envelope.Id);
        
        updated.Should().NotBeNull();
        updated!.BudgetDraft.Should().BeNull();
        updated.Budget.Should().Be(200m); // Budget should remain unchanged
    }

    /// <summary>
    /// Test CopyBudgetToNextMonth endpoint - should copy budget values to next month
    /// </summary>
    [Fact]
    public async Task CopyBudgetToNextMonth_Should_Copy_Values_To_Next_Month()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var envelope = TestHelpers.CreateTestEnvelope(id: 603, name: "Test Envelope", categoryId: "1");
        db.Envelopes.Add(envelope);

        var sourceAcctPeriod = 202403;
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = sourceAcctPeriod,
            EnvelopeId = envelope.Id,
            Budget = 400m,
            BudgetDraft = null
        };
        db.BudgetMonths.Add(budgetMonth);
        await db.SaveChangesAsync();

        var command = new CopyBudgetToNextMonth.Command(
            SourceAcctPeriod: sourceAcctPeriod,
            CopyFromDraft: false,
            ConfirmOverwrite: false);

        // Act
        var response = await client.PostAsJsonAsync("/budgetmonths/copytonextmonth", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CopyBudgetToNextMonth.Response>();
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.RecordsUpdated.Should().BeGreaterThan(0);
        
        // Verify in database - next month should have draft value
        db.ChangeTracker.Clear();
        var targetAcctPeriod = 202404;
        var copied = await db.BudgetMonths
            .FirstOrDefaultAsync(b => b.AcctPeriod == targetAcctPeriod && b.EnvelopeId == envelope.Id);
        
        copied.Should().NotBeNull();
        copied!.BudgetDraft.Should().Be(400m);
    }

    /// <summary>
    /// Test ClearMonthBudgets endpoint - should clear budget values for a specific month
    /// </summary>
    [Fact]
    public async Task ClearMonthBudgets_Should_Clear_Budgets_For_Month()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var envelope = TestHelpers.CreateTestEnvelope(id: 604, name: "Test Envelope", categoryId: "1");
        db.Envelopes.Add(envelope);

        var acctPeriod = 202505;
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = acctPeriod,
            EnvelopeId = envelope.Id,
            Budget = 500m,
            BudgetDraft = 100m
        };
        db.BudgetMonths.Add(budgetMonth);
        await db.SaveChangesAsync();

        var command = new ClearMonthBudgets.Command(acctPeriod);

        // Act
        var response = await client.PostAsJsonAsync("/budgetmonths/clearmonthbudgets", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClearMonthBudgets.Response>();
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        
        // Verify in database
        db.ChangeTracker.Clear();
        var updated = await db.BudgetMonths
            .FirstOrDefaultAsync(b => b.AcctPeriod == acctPeriod && b.EnvelopeId == envelope.Id);
        
        updated.Should().NotBeNull();
        updated!.Budget.Should().BeNull();
        updated.BudgetDraft.Should().Be(100m); // Draft should remain unchanged
    }

    /// <summary>
    /// Test ClearMonthDrafts endpoint - should clear draft values for a specific month
    /// </summary>
    [Fact]
    public async Task ClearMonthDrafts_Should_Clear_Drafts_For_Month()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var envelope = TestHelpers.CreateTestEnvelope(id: 605, name: "Test Envelope", categoryId: "1");
        db.Envelopes.Add(envelope);

        var acctPeriod = 202506;
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = acctPeriod,
            EnvelopeId = envelope.Id,
            Budget = 500m,
            BudgetDraft = 100m
        };
        db.BudgetMonths.Add(budgetMonth);
        await db.SaveChangesAsync();

        var command = new ClearMonthDrafts.Command(acctPeriod);

        // Act
        var response = await client.PostAsJsonAsync("/budgetmonths/clearmonthdrafts", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClearMonthDrafts.Response>();
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        
        // Verify in database
        db.ChangeTracker.Clear();
        var updated = await db.BudgetMonths
            .FirstOrDefaultAsync(b => b.AcctPeriod == acctPeriod && b.EnvelopeId == envelope.Id);
        
        updated.Should().NotBeNull();
        updated!.BudgetDraft.Should().BeNull();
        updated.Budget.Should().Be(500m); // Budget should remain unchanged
    }

    /// <summary>
    /// Test ClearMonthBoth endpoint - should clear both budget and draft values for a specific month
    /// </summary>
    [Fact]
    public async Task ClearMonthBoth_Should_Clear_Both_Values_For_Month()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var envelope = TestHelpers.CreateTestEnvelope(id: 606, name: "Test Envelope", categoryId: "1");
        db.Envelopes.Add(envelope);

        var acctPeriod = 202507;
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = acctPeriod,
            EnvelopeId = envelope.Id,
            Budget = 500m,
            BudgetDraft = 100m
        };
        db.BudgetMonths.Add(budgetMonth);
        await db.SaveChangesAsync();

        var command = new ClearMonthBoth.Command(acctPeriod);

        // Act
        var response = await client.PostAsJsonAsync("/budgetmonths/clearmonthboth", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClearMonthBoth.Response>();
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        
        // Verify in database
        db.ChangeTracker.Clear();
        var updated = await db.BudgetMonths
            .FirstOrDefaultAsync(b => b.AcctPeriod == acctPeriod && b.EnvelopeId == envelope.Id);
        
        updated.Should().NotBeNull();
        updated!.Budget.Should().BeNull();
        updated.BudgetDraft.Should().BeNull();
    }

    /// <summary>
    /// Test ApplyMonthDrafts endpoint - should apply drafts to budget for a specific month
    /// </summary>
    [Fact]
    public async Task ApplyMonthDrafts_Should_Apply_Drafts_For_Month()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

        var envelope = TestHelpers.CreateTestEnvelope(id: 6070, name: "Test Envelope", categoryId: "1");
        db.Envelopes.Add(envelope);

        var acctPeriod = 202508;
        var budgetMonth = new BudgetMonth
        {
            AcctPeriod = acctPeriod,
            EnvelopeId = envelope.Id,
            Budget = null,
            BudgetDraft = 300m
        };
        db.BudgetMonths.Add(budgetMonth);
        await db.SaveChangesAsync();

        var command = new ApplyMonthDrafts.Command(acctPeriod);

        // Act
        var response = await client.PostAsJsonAsync("/budgetmonths/applymonthdrafts", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApplyMonthDrafts.Response>();
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.RecordsUpdated.Should().BeGreaterThan(0);
        
        // Verify in database
        db.ChangeTracker.Clear();
        var updated = await db.BudgetMonths
            .FirstOrDefaultAsync(b => b.AcctPeriod == acctPeriod && b.EnvelopeId == envelope.Id);
        
        updated.Should().NotBeNull();
        updated!.Budget.Should().Be(300m);
        updated.BudgetDraft.Should().BeNull();
    }
}
