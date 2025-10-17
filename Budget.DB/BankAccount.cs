using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Budget.DB
{
  public class BankAccount
  {
    public enum AccountTypes { Checking, Credit }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 0m;
    public AccountTypes AccountType { get; set; } = AccountTypes.Checking;

    public DateTime LastTransactionDate { get; set; }
    public int LastTransactionId { get; set; }

    public List<Transaction> Transactions { get; set; } = [];


    public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
    {
      public void Configure(EntityTypeBuilder<BankAccount> entity)
      {
        entity.Property(u => u.Name)
              .HasMaxLength(50);

        // Ensure SQL column type can hold your money values (SQL Server)
        entity.Property(u => u.Balance)
              .HasPrecision(18, 2); // translates to decimal(18,2)

        entity.HasData(
          new BankAccount() { Id = 1, Name = "Citizens", AccountType = AccountTypes.Checking },
          new BankAccount() { Id = 2, Name = "Discover", AccountType = AccountTypes.Credit }
        );
      }
    }
  }
}