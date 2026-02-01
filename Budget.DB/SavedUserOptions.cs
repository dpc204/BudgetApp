using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Budget.Shared.Enums;

namespace Budget.DB
{
  public class SavedUserOptions
  {
    [Key]
    public int UserId { get; set; }
    public string? JsonOptions { get; set; }

    public class SavedUserOptionsConfiguration : IEntityTypeConfiguration<SavedUserOptions>
    {
      public void Configure(EntityTypeBuilder<SavedUserOptions> entity)
      {
        entity.Property(e => e.UserId)
          .ValueGeneratedNever(); // UserId is not auto-generated, it's a FK to User
        entity.Property(a => a.JsonOptions)
          .HasMaxLength(1000);
      }
    }
  }
}