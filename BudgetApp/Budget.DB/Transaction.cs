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
          new Transaction  {Id = 1, Date = new DateTime(2023, 1, 1), Description = "New Year's Dinner", TotalAmount = 100.00m, UserId = 1 },
          new Transaction {Id = 2, Date = new DateTime(2023, 1, 2), Description = "Groceries", TotalAmount = 50.00m, UserId = 1 },
          new Transaction {Id = 3, Date = new DateTime(2023, 1, 3), Description = "Gas", TotalAmount = 30.00m, UserId = 2 }
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
          new TransactionDetail {Amount = 52m , LineId = 1, TransactionId =1, EnvelopeId = 1},
          new TransactionDetail {Amount = 48m , LineId = 2, TransactionId =1, EnvelopeId = 2},
          new TransactionDetail { Amount = 50m, LineId = 1, TransactionId = 2, EnvelopeId = 3 },
          new TransactionDetail { Amount = 27m, LineId = 1, TransactionId = 3, EnvelopeId = 3 },
          new TransactionDetail { Amount = 3m, LineId = 2, TransactionId = 3, EnvelopeId = 2 }
        );
      }
    }
  }
}