namespace Budget.Shared.Models;


public class BankAccountDto
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public decimal Balance { get; set; }
  public AccountTypes AccountType { get; set; } = AccountTypes.Checking;
}
