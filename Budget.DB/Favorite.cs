using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB
{
  public class Favorite
  {
    public enum FavoriteTypes
    {
      Envelope
    }

    public int Id { get; set; }
    public FavoriteTypes FavoriteType { get; set; }

    // FK and navigation to User
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
    {
      public void Configure(EntityTypeBuilder<Favorite> entity)
      {
        entity.Property(f => f.FavoriteType)
          .HasConversion<string>()
          .HasMaxLength(32);

        // Optional index (helps queries by user)
        entity.HasIndex(f => f.UserId);

        // FK: Favorite.UserId -> User.Id
        entity.HasOne(f => f.User)
          .WithMany(u => u.Favorites)
          .HasForeignKey(f => f.UserId)
          .OnDelete(DeleteBehavior.Cascade);
      }
    }
  }
}