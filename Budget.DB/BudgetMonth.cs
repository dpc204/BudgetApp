using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB;

/// <summary>
/// Represents budget data for an envelope in a specific month
/// </summary>
public class BudgetMonth
{
  public int AcctPeriod { get; set; }
  public int EnvelopeId { get; set; }
  public Envelope Envelope { get; set; } = null!;
  public decimal Budget { get; set; }
  public decimal? BudgetDraft { get; set; }

  /// <summary>
  /// Converts a DateTime to AcctPeriod format (YYYYMM)
  /// </summary>
  public static int DateToAcctPeriod(DateTime date)
  {
    return date.Year * 100 + date.Month;
  }

  /// <summary>
  /// Converts AcctPeriod format (YYYYMM) to DateTime (first of month)
  /// </summary>
  public static DateTime AcctPeriodToDate(int acctPeriod)
  {
    var year = acctPeriod / 100;
    var month = acctPeriod % 100;
    return new DateTime(year, month, 1);
  }

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
