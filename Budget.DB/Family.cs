using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB;

/// <summary>
/// Represents a family unit for multi-tenancy data isolation
/// </summary>
public class Family
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

  public class FamilyConfiguration : IEntityTypeConfiguration<Family>
  {
    public void Configure(EntityTypeBuilder<Family> entity)
    {
      entity.Property(f => f.Name)
        .HasMaxLength(100)
        .IsRequired();

      // Seed default family
      entity.HasData(
        new Family { Id = 1, Name = "Default Family", CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
      );
    }
  }
}
