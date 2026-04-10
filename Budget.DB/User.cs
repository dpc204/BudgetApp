using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB
{
  public class User
  {
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int FamilyId { get; set; } = 1;
    public Family Family { get; set; } = null!;

    // Back-reference collection
    public List<Transaction> Transactions { get; set; } = [];
    public List<Favorite> Favorites { get; set; } = [];
    public List<UserRole> UserRoles { get; set; } = [];

    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
      public void Configure(EntityTypeBuilder<User> entity)
      {
        // Configure table to use triggers (prevents EF from using OUTPUT clause)
        entity.ToTable(tb => tb.HasTrigger("trg_User_Email_ToUpper"));

        entity.Property(u => u.Email)
          .HasMaxLength(100);
        entity.Property(u => u.FirstName)
          .HasMaxLength(50);
        entity.Property(u => u.LastName)
          .HasMaxLength(50);

        entity.HasOne(u => u.Family)
          .WithMany()
          .HasForeignKey(u => u.FamilyId)
          .OnDelete(DeleteBehavior.Restrict);

        entity.HasData(
          new User { Id = 1, Email = "", FirstName = "Patrick", LastName = "Connelly", FamilyId = 1 },
          new User { Id = 2, Email = "", FirstName = "Terri", LastName = "Connelly", FamilyId = 1 }
        );
      }
    }
  }

  public enum UserTypes
  {
    Standard = 0,
    System = 1,
  }
}