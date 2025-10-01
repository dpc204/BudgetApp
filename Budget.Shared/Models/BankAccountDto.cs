namespace Budget.Shared.Models;

public enum BankAccountType
{
  Checking = 0,
  Credit = 1
}

public class BankAccountDto
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public decimal Balance { get; set; }
  public BankAccountType AccountType { get; set; } = BankAccountType.Checking;
}
