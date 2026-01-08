using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Budget.Api.Features.Accounts.AccountMaint;
using Budget.DB;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using GetAll = Budget.Api.Features.Accounts.AccountMaint.GetAll;

namespace Budget.ApiTests;

/// <summary>
/// Tests for Account API endpoints
/// </summary>
public class AccountEndpointsTests : IntegrationTestBase
{

  /// <summary>
  /// Test GetAccounts endpoint - should return all accounts
  /// </summary>
  [Fact]
  public async Task GetAccounts_Should_Return_All_Accounts()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      var account1 = TestHelpers.CreateTestAccount(id: 300, name: "Checking", balance: 1000m);
      var account2 = TestHelpers.CreateTestAccount(id: 301, name: "Savings", balance: 5000m);

      db.BankAccounts.Add(account1);
      db.BankAccounts.Add(account2);
      await db.SaveChangesAsync();

      // Act
      var response = await Client.GetAsync("/accounts/maint/getall");
      
      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<List<GetAll.Response>>();

      result.Should().NotBeNull();
      result.Should().HaveCount(c => c >= 2);

      var acct1 = result!.FirstOrDefault(a => a.Id == 300);
      acct1.Should().NotBeNull();
      acct1!.Name.Should().Be("Checking");
      acct1.Balance.Should().Be(1000m);

      var acct2 = result.FirstOrDefault(a => a.Id == 301);
      acct2.Should().NotBeNull();
      acct2!.Name.Should().Be("Savings");
      acct2.Balance.Should().Be(5000m);
    }
  }

  /// <summary>
  /// Test InsertAccount endpoint - should create a new account
  /// </summary>
  [Fact]
  public async Task InsertAccount_Should_Create_New_Account()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var command = new InsertAccount.Command(
          Name: "New Account",
          Balance: 2500m,
          AccountType: BankAccount.AccountTypes.Checking);

      // Act
      var response = await Client.PostAsJsonAsync("/accounts/maint/Insert", command);

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<InsertAccount.Response>();

      result.Should().NotBeNull();
      result!.Name.Should().Be("New Account");
      result.Balance.Should().Be(2500m);
      result.AccountType.Should().Be(BankAccount.AccountTypes.Checking);
      result.Id.Should().BeGreaterThan(0);

      // Verify in database
      db.ChangeTracker.Clear();
      var savedAccount = await db.BankAccounts.FindAsync(result.Id);

      savedAccount.Should().NotBeNull();
      savedAccount!.Name.Should().Be("New Account");
    }
  }

  /// <summary>
  /// Test UpdateAccount endpoint - should update an existing account
  /// </summary>
  [Fact]
  public async Task UpdateAccount_Should_Update_Existing_Account()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      var account = TestHelpers.CreateTestAccount(id: 302, name: "Original Name", balance: 1000m);
      db.BankAccounts.Add(account);
      await db.SaveChangesAsync();

      var commandBody = new UpdateAccount.CommandBody
      {
        Id = 302,
        Name = "Updated Name",
        Balance = 1500m,
        AccountType = BankAccount.AccountTypes.Credit
      };

      // Act
      var response = await Client.PutAsJsonAsync("/accounts/maint/302", commandBody);

      // Assert
      response.EnsureSuccessStatusCode();
      var result = await response.Content.ReadFromJsonAsync<UpdateAccount.Response>();

      result.Should().NotBeNull();
      result!.Id.Should().Be(302);
      result.Name.Should().Be("Updated Name");
      result.Balance.Should().Be(1500m);
      result.AccountType.Should().Be(BankAccount.AccountTypes.Credit);

      // Verify in database
      db.ChangeTracker.Clear();
      var updatedAccount = await db.BankAccounts.FindAsync(302);

      updatedAccount.Should().NotBeNull();
      updatedAccount!.Name.Should().Be("Updated Name");
      updatedAccount.Balance.Should().Be(1500m);
    }
  }

  /// <summary>
  /// Test UpdateAccount endpoint with mismatched IDs - should return BadRequest
  /// </summary>
  [Fact]
  public async Task UpdateAccount_With_Mismatched_Ids_Should_Return_BadRequest()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var commandBody = new UpdateAccount.CommandBody
      {
        Id = 999,
        Name = "Test",
        Balance = 100m,
        AccountType = BankAccount.AccountTypes.Checking
      };

      // Act
      var response = await Client.PutAsJsonAsync("/accounts/maint/303", commandBody);

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
  }

  /// <summary>
  /// Test UpdateAccount endpoint with non-existent account - should return NotFound
  /// </summary>
  [Fact]
  public async Task UpdateAccount_With_NonExistent_Account_Should_Return_NotFound()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();
      var commandBody = new UpdateAccount.CommandBody
      {
        Id = 99999,
        Name = "Test",
        Balance = 100m,
        AccountType = BankAccount.AccountTypes.Checking
      };

      // Act
      var response = await Client.PutAsJsonAsync("/accounts/maint/99999", commandBody);

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
  }

  /// <summary>
  /// Test RemoveAccount endpoint - should delete an account
  /// </summary>
  [Fact]
  public async Task RemoveAccount_Should_Delete_Account()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      var account = TestHelpers.CreateTestAccount(id: 304, name: "To Delete", balance: 500m);
      db.BankAccounts.Add(account);
      await db.SaveChangesAsync();

      // Act
      var response = await Client.DeleteAsync("/accounts/maint/304");

      // Assert
      response.EnsureSuccessStatusCode();
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

      // Verify deletion in database
      db.ChangeTracker.Clear();
      var deletedAccount = await db.BankAccounts.FindAsync(304);
      deletedAccount.Should().BeNull();
    }
  }

  /// <summary>
  /// Test RemoveAccount endpoint with non-existent account - should return NotFound
  /// </summary>
  [Fact]
  public async Task RemoveAccount_With_NonExistent_Account_Should_Return_NotFound()
  {
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<BudgetContext>();

      // Act
      var response = await Client.DeleteAsync("/accounts/maint/99999");

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
  }
}
