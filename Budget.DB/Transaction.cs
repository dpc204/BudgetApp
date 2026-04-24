using Budget.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.DB
{
  public class Transaction
  {
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public PostingStatuses PostingStatus { get; set; }

    public TransactionTypes TransactionType { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    [ForeignKey("Account")]
    public int AccountId { get; set; }
    public BankAccount Account { get; set; } = null!;
    [ForeignKey("User")] public int UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsVoided { get; set; }
    public int FamilyId { get; set; } = 1;
    public bool WasPotentialDuplicate { get; set; }
    public bool TransactionHiddenFromAssign { get; set; }
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
    public string? Notes { get; set; } = string.Empty;
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

      }
    }
  }


}