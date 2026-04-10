using Budget.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB;

/// <summary>
/// Staging table for imported transactions before final import
/// </summary>
public partial class TransactionImport
{
  public int Id { get; set; }
  public DateTime Date { get; set; }
  public string Vendor { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public string Notes { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public int EnvelopeId { get; set; }
  public string EnvelopeName { get; set; } = string.Empty;
  public int UserId { get; set; }
  public int FamilyId { get; set; }
  public Family Family { get; set; } = null!;
  public DateTime ImportedAt { get; set; }
  public bool Duplicate { get; set; } = false;
  public PostingStatuses PostingStatus { get; set; }
  public bool KeepDuplicate { get; set; }


  public class TransactionImportConfiguration : IEntityTypeConfiguration<TransactionImport>
  {
    public void Configure(EntityTypeBuilder<TransactionImport> entity)
    {
      entity.Property(t => t.Vendor)
        .HasMaxLength(200);

      entity.Property(t => t.Description)
        .HasMaxLength(500);

      entity.Property(t => t.Notes)
        .HasMaxLength(500);

      entity.Property(t => t.EnvelopeName)
        .HasMaxLength(200);

      entity.Property(t => t.Amount)
        .HasPrecision(18, 2);

      entity.HasOne(t => t.Family)
        .WithMany()
        .HasForeignKey(t => t.FamilyId)
        .OnDelete(DeleteBehavior.Restrict);

      entity.HasIndex(t => t.FamilyId);
      entity.HasIndex(t => t.ImportedAt);
    }
  }

}