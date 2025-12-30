using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Budget.Shared.Enums;

namespace Budget.DB
{
  public class SavedUserOptions
  {
    [Key]
    public string UserId { get; set; } = string.Empty;
    public string? JsonOptions { get; set; }

    public class SavedUserOptionsConfiguration : IEntityTypeConfiguration<SavedUserOptions>
    {
      public void Configure(EntityTypeBuilder<SavedUserOptions> entity)
      {
        entity.Property(e => e.UserId)
          .HasMaxLength(100);
        entity.Property(a => a.JsonOptions)
          .HasMaxLength(1000);
      }
    }
  }
}