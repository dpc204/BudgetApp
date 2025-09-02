using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB
{
  public class Envelope
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Budget { get; set; } = 0;
    public decimal Balance { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public List<TransactionDetail> Details { get; set; } = new();

    public class EnvelopeConfiguration : IEntityTypeConfiguration<Envelope>
    {
      public void Configure(EntityTypeBuilder<Envelope> entity)
      {
        entity.Property(e => e.Name)
          .HasMaxLength(100);
        entity.Property(a => a.Description)
          .HasMaxLength(500);

        entity.HasData(new Envelope { Id = 1, Name = "Dining Out", },
          new Envelope { Id = 2, Name = "Groceries" },
          new Envelope { Id = 3, Name = "Gas" }
        );
      }
    }
  }
}