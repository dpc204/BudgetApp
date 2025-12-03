using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Budget.Shared.Utilities;

namespace Budget.DB;

/// <summary>
/// Represents budget data for an envelope in a specific month
/// </summary>
public class BudgetMonth
{
  public int AcctPeriod { get; set; }
  public int EnvelopeId { get; set; }
  public Envelope Envelope { get; set; } = null!;
  public decimal? Budget { get; set; }
  public decimal? BudgetDraft { get; set; }

  public class BudgetMonthConfiguration : IEntityTypeConfiguration<BudgetMonth>
  {
    public void Configure(EntityTypeBuilder<BudgetMonth> entity)
    {
      // Composite primary key
      entity.HasKey(b => new { b.AcctPeriod, b.EnvelopeId });

      entity.Property(b => b.Budget)
        .HasPrecision(18, 2);

      entity.Property(b => b.BudgetDraft)
        .HasPrecision(18, 2);

      // Foreign key relationship
      entity.HasOne(b => b.Envelope)
        .WithMany()
        .HasForeignKey(b => b.EnvelopeId)
        .OnDelete(DeleteBehavior.Cascade);
    }
  }
}
