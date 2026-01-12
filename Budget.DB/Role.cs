using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB;

/// <summary>
/// Represents an application role for authorization
/// </summary>
public class Role
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public DateTime? ModifiedAt { get; set; }

  // Navigation properties
  public List<UserRole> UserRoles { get; set; } = [];

  public class RoleConfiguration : IEntityTypeConfiguration<Role>
  {
    public void Configure(EntityTypeBuilder<Role> entity)
    {
      entity.HasKey(r => r.Id);

      entity.Property(r => r.Name)
        .IsRequired()
        .HasMaxLength(50);

      entity.HasIndex(r => r.Name)
        .IsUnique();

      entity.Property(r => r.Description)
        .HasMaxLength(200);

      entity.Property(r => r.CreatedAt)
        .HasDefaultValueSql("GETUTCDATE()");

      // Seed default roles
      entity.HasData(
        new Role
        {
          Id = 1,
          Name = "Admin",
          Description = "Full system access including user and role management",
          CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Role
        {
          Id = 2,
          Name = "PowerUser",
          Description = "Advanced features including import and maintenance",
          CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Role
        {
          Id = 3,
          Name = "User",
          Description = "Standard user with budget management access",
          CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }
      );
    }
  }
}
