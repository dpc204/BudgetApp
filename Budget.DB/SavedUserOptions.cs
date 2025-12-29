using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB;

/// <summary>
/// Represents saved user options stored as JSON in the database
/// </summary>
public class SavedUserOptions
{
  /// <summary>
  /// The user ID (primary key)
  /// </summary>
  public string UserId { get; set; } = string.Empty;

  /// <summary>
  /// JSON representation of user options
  /// </summary>
  public string JsonOptions { get; set; } = string.Empty;

  /// <summary>
  /// Entity configuration for SavedUserOptions
  /// </summary>
  public class SavedUserOptionsConfiguration : IEntityTypeConfiguration<SavedUserOptions>
  {
    public void Configure(EntityTypeBuilder<SavedUserOptions> entity)
    {
      entity.HasKey(s => s.UserId);
      
      entity.Property(s => s.UserId)
        .HasMaxLength(450)
        .IsRequired();
      
      entity.Property(s => s.JsonOptions)
        .IsRequired();
    }
  }
}
