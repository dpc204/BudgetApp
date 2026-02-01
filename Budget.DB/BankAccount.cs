using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using Budget.Shared.Enums;

namespace Budget.DB
{
  public class BankAccount
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 0m;
    public AccountTypes AccountType { get; set; } = AccountTypes.Checking;
    public int FamilyId { get; set; } = 1;
    public Family Family { get; set; } = null!;

    public DateTime LastTransactionDate { get; set; }

    [ForeignKey("Transaction")]
    public int? LastTransactionId { get; set; }
    public Transaction? LastTransaction { get; set; }


    public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
    {
      public void Configure(EntityTypeBuilder<BankAccount> entity)
      {
        entity.Property(u => u.Name)
              .HasMaxLength(50);

        // Ensure SQL column type can hold your money values (SQL Server)
        entity.Property(u => u.Balance)
              .HasPrecision(18, 2); // translates to decimal(18,2)

        // Optional FK to last transaction; allow null for existing rows and SetNull on delete
        entity.HasOne(b => b.LastTransaction)
              .WithMany()
              .HasForeignKey(b => b.LastTransactionId)
              .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(b => b.Family)
              .WithMany()
              .HasForeignKey(b => b.FamilyId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasData(
          new BankAccount() { Id = 1, Name = "Citizens", AccountType = AccountTypes.Checking, FamilyId = 1 },
          new BankAccount() { Id = 2, Name = "Discover", AccountType = AccountTypes.Credit, FamilyId = 1 }
        );
      }
    }
  }
}