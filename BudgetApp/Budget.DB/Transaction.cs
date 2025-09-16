using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB
{
  public class Transaction
  {
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal BalanceAfterTransaction { get; set; }
    public List<TransactionDetail> Details { get; set; } = [];

    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
      public void Configure(EntityTypeBuilder<Transaction> entity)
      {
        entity.Property(t => t.Description)
          .HasMaxLength(200);
        entity.Property(t => t.UserId)
          .HasMaxLength(50);
        entity.Property(t => t.TotalAmount)
          .HasPrecision(18, 2);
        entity.Property(t => t.BalanceAfterTransaction)
          .HasPrecision(18, 2);

        entity.HasData(
          new Transaction { Id = 1, Date = new DateTime(2023, 1, 1), Description = "Giant", TotalAmount = 104.00m, UserId = 1 },
          new Transaction { Id = 2, Date = new DateTime(2023, 1, 1), Description = "Bonefish", TotalAmount = 48m, UserId = 1 },
          new Transaction { Id = 3, Date = new DateTime(2023, 1, 2), Description = "Gas", TotalAmount = 12.50m, UserId = 1 },
          new Transaction { Id = 4, Date = new DateTime(2023, 1, 3), Description = "Home Depot", TotalAmount = 30.00m, UserId = 2 },
          new Transaction { Id = 5, Date = new DateTime(2023, 1, 3), Description = "CVS", TotalAmount = 32.00m, UserId = 2 }
        );
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

        entity.HasData(
          new TransactionDetail { TransactionId = 1,LineId = 1, Amount = 52m,    EnvelopeId = 2, Notes = "Yasso" },
          new TransactionDetail { TransactionId = 1,LineId = 2, Amount = 52m,    EnvelopeId = 6, Notes = "Cough supresent" },
          new TransactionDetail { TransactionId = 2,LineId = 1, Amount = 48m,    EnvelopeId = 1, Notes = "din din" },
          new TransactionDetail { TransactionId = 3,LineId = 1, Amount = 10m,    EnvelopeId = 3 },
          new TransactionDetail { TransactionId = 3,LineId = 2, Amount = 2.5m,  EnvelopeId = 2, Notes = "Tic Tacs" },
          new TransactionDetail { TransactionId = 4,LineId = 1, Amount = 27m,    EnvelopeId = 5, Notes = "Plumbing" },
          new TransactionDetail { TransactionId = 4,LineId = 2, Amount = 3m,      EnvelopeId = 2, Notes = "Candy" },
          new TransactionDetail { TransactionId = 5,LineId = 1, Amount = 20m,    EnvelopeId = 6, Notes = "Prescriptions" },
          new TransactionDetail { TransactionId = 5,LineId = 2, Amount = 4,        EnvelopeId = 2, Notes = "Gum" },
          new TransactionDetail { TransactionId = 5,LineId = 3, Amount = 8m,      EnvelopeId = 5, Notes = "Light Bulbs" }
        );
      }
    }
  }
}