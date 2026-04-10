using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB;

/// <summary>
/// Junction table linking Users to Roles
/// </summary>
public class UserRole
{
  public int UserId { get; set; }
  public int RoleId { get; set; }
  public DateTime AssignedAt { get; set; }
  public int? AssignedByUserId { get; set; }

  // Navigation properties
  public User User { get; set; } = null!;
  public Role Role { get; set; } = null!;
  public User? AssignedBy { get; set; }

  public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
  {
    public void Configure(EntityTypeBuilder<UserRole> entity)
    {
      entity.HasKey(ur => new { ur.UserId, ur.RoleId });

      entity.HasOne(ur => ur.User)
        .WithMany(u => u.UserRoles)
        .HasForeignKey(ur => ur.UserId)
        .OnDelete(DeleteBehavior.Cascade);

      entity.HasOne(ur => ur.Role)
        .WithMany(r => r.UserRoles)
        .HasForeignKey(ur => ur.RoleId)
        .OnDelete(DeleteBehavior.Cascade);

      entity.HasOne(ur => ur.AssignedBy)
        .WithMany()
        .HasForeignKey(ur => ur.AssignedByUserId)
        .OnDelete(DeleteBehavior.NoAction);

      entity.Property(ur => ur.AssignedAt)
        .HasDefaultValueSql("GETUTCDATE()");

      // Seed initial admin role for user 1 (Patrick)
      entity.HasData(
        new UserRole {
          UserId = 1,
          RoleId = 1, // Admin
          AssignedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new UserRole {
          UserId = 2,
          RoleId = 3, // User
          AssignedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }
      );
    }
  }
}
