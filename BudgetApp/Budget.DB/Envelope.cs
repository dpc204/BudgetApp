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
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal Balance { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }


    public List<TransactionDetail> Details { get; set; } = [];

    public class EnvelopeConfiguration : IEntityTypeConfiguration<Envelope>
    {
      public void Configure(EntityTypeBuilder<Envelope> entity)
      {
        entity.Property(e => e.Name)
          .HasMaxLength(100);
        entity.Property(a => a.Description)
          .HasMaxLength(500);
        entity.Property(u => u.Balance)
          .HasPrecision(18, 2); // translates to decimal(18,2)
        entity.Property(u => u.Budget)
          .HasPrecision(18, 2); // translates to decimal(18,2)

        // Seed only scalar + FK values; no navigation instances
        entity.HasData(
          new Envelope { Id = 1, Name = "Dining Out", CategoryId = 1, SortOrder = 1 },
          new Envelope { Id = 2, Name = "Groceries", CategoryId = 1, SortOrder = 2 },
          new Envelope { Id = 3, Name = "Gas", CategoryId = 1, SortOrder = 3 },
          new Envelope { Id = 4, Name = "Car Maint", CategoryId = 2, SortOrder = 4 },
          new Envelope { Id = 5, Name = "House Maint", CategoryId = 2, SortOrder = 5 },                                                                     
          new Envelope { Id = 6, Name = "Medical", CategoryId = 2, SortOrder = 5 } 
        );
      }
    }
  }
}