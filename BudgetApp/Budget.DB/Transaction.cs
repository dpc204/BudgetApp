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
    public string UserId { get; set; } = string.Empty;
    public List<TransactionDetail> Details { get; set; } = new();

    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
      public void Configure(EntityTypeBuilder<Transaction> entity)
      {
        entity.Property(t => t.Description)
              .HasMaxLength(200);
        entity.Property(t => t.UserId)
              .HasMaxLength(50);
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
      }
    }
  }
}