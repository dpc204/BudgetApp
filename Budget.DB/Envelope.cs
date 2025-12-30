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
    public decimal? Budget { get; set; }
    public decimal Balance { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime LastTransactionDate { get; set; }
    public EnvelopeTypes EnvelopeType { get; set; }
    public int FamilyId { get; set; } = 1;
    public Family Family { get; set; } = null!;

    public int? LastTransactionId { get; set; }
    public int? LastTransactionLineId { get; set; }
    public TransactionDetail? LastTransactionDetail { get; set; }


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

        // Configure one-to-one pointer to the last transaction detail
        // FK lives on Envelope (LastTransactionId, LastTransactionLineId) -> TransactionDetail (TransactionId, LineId)
        entity.HasOne(e => e.LastTransactionDetail)
              .WithMany()
              .HasForeignKey(e => new { e.LastTransactionId, e.LastTransactionLineId })
              .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(e => e.Family)
              .WithMany()
              .HasForeignKey(e => e.FamilyId)
              .OnDelete(DeleteBehavior.Restrict);

        // Seed only scalar + FK values; no navigation instances
        entity.HasData(
          new Envelope { Id = 1, Name = "Dining Out", CategoryId = 1, SortOrder = 1, FamilyId = 1 },
          new Envelope { Id = 2, Name = "Groceries", CategoryId = 1, SortOrder = 2, FamilyId = 1 },
          new Envelope { Id = 3, Name = "Gas", CategoryId = 1, SortOrder = 3, FamilyId = 1 },
          new Envelope { Id = 4, Name = "Car Maint", CategoryId = 2, SortOrder = 4, FamilyId = 1 },
          new Envelope { Id = 5, Name = "House Maint", CategoryId = 2, SortOrder = 5, FamilyId = 1 },
          new Envelope { Id = 6, Name = "Medical", CategoryId = 2, SortOrder = 5, FamilyId = 1 },
          new Envelope { Id = -1, Name = "UnAllocated", CategoryId = -1, SortOrder = 6, FamilyId = 1 }
        );
      }
    }
  }

  public enum EnvelopeTypes
  {
    Unallocated,
    Standard
  }
}