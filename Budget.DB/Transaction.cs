using System.ComponentModel.DataAnnotations.Schema;
using Budget.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB
{
  public class Transaction
  {
    public int Id { get; set; }
    public DateTime Date { get; set; }
    
    public string Vendor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    [ForeignKey("Account")]
    public int AccountId { get; set; }
    public BankAccount Account { get; set; } = null!;
    public string UserName { get; set; } = string.Empty;

    [ForeignKey("User")] public int UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal BalanceAfterTransaction { get; set; }
    public bool IsVoided { get; set; }
    public int FamilyId { get; set; } = 1;
    public bool WasPotentialDuplicate { get; set; } 
    public Family Family { get; set; } = null!;
    public List<TransactionDetail> Details { get; set; } = [];

    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
      public void Configure(EntityTypeBuilder<Transaction> entity)
      {
        entity.Property(t => t.Vendor)
          .HasMaxLength(200).IsRequired(); 
        entity.Property(t => t.Description)
          .HasMaxLength(200);
        entity.Property(t => t.TotalAmount)
          .HasPrecision(18, 2);
        entity.Property(t => t.BalanceAfterTransaction)
          .HasPrecision(18, 2);



        // Explicit relationships ensure principal data is seeded first
        entity.HasOne(t => t.Account)
              .WithMany() // no navigation collection on BankAccount
              .HasForeignKey(t => t.AccountId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(t => t.User)
          .WithMany(u => u.Transactions)
          .HasForeignKey(t => t.UserId)
          .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(t => t.Family)
          .WithMany()
          .HasForeignKey(t => t.FamilyId)
          .OnDelete(DeleteBehavior.Restrict);

        
      }
    }
  }

  public class TransactionDetail
  {
    public int TransactionId { get; set; }
    public int LineId { get; set; }
    public Transaction Transaction { get; set; } = null!;
    public int EnvelopeId { get; set; }
    public Envelope Envelope { get; set; } = null!;
    public string Notes { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public class TransactionDetailConfiguration : IEntityTypeConfiguration<TransactionDetail>
    {
      public void Configure(EntityTypeBuilder<TransactionDetail> entity)
      {
        entity.Property(td => td.Notes)
          .HasMaxLength(500);
        entity.HasKey(c => new { c.TransactionId, c.LineId });
        entity.Property(t => t.Amount)
          .HasPrecision(18, 2);

        entity.HasOne(t => t.Envelope)
          .WithMany(en => en.Details)
          .HasForeignKey(t => t.EnvelopeId)
          .OnDelete(DeleteBehavior.Restrict);

        // Document trigger for EF Core model (ensures it's included in future Initial migrations)
        entity.ToTable(tb => tb.HasTrigger("trg_TransactionDetails_UpdateEnvelopeBalance"));

        //entity.HasData(
        //  new TransactionDetail { TransactionId = 1, LineId = 1, Amount = 52m, EnvelopeId = 2, Notes = "Yasso" },
        //  new TransactionDetail
        //    { TransactionId = 1, LineId = 2, Amount = 52m, EnvelopeId = 6, Notes = "Cough supresent" },
        //  new TransactionDetail { TransactionId = 2, LineId = 1, Amount = 48m, EnvelopeId = 1, Notes = "din din" },
        //  new TransactionDetail { TransactionId = 3, LineId = 1, Amount = 10m, EnvelopeId = 3 },
        //  new TransactionDetail { TransactionId = 3, LineId = 2, Amount = 2.5m, EnvelopeId = 2, Notes = "Tic Tacs" },
        //  new TransactionDetail { TransactionId = 4, LineId = 1, Amount = 27m, EnvelopeId = 5, Notes = "Plumbing" },
        //  new TransactionDetail { TransactionId = 4, LineId = 2, Amount = 3m, EnvelopeId = 2, Notes = "Candy" },
        //  new TransactionDetail
        //    { TransactionId = 5, LineId = 1, Amount = 20m, EnvelopeId = 6, Notes = "Prescriptions" },
        //  new TransactionDetail { TransactionId = 5, LineId = 2, Amount = 4, EnvelopeId = 2, Notes = "Gum" },
        //  new TransactionDetail { TransactionId = 5, LineId = 3, Amount = 8m, EnvelopeId = 5, Notes = "Light Bulbs" }
        //);
      }
    }
  }
}