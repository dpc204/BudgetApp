using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Budget.Shared.Enums;

namespace Budget.DB
{
  public class Category
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public CatTypes CategoryType { get; set; }
    public int FamilyId { get; set; } = 1;
    public Family Family { get; set; } = null!;
    public List<Envelope> Envelopes { get; set; } = [];

    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
      public void Configure(EntityTypeBuilder<Category> entity)
      {
        entity.Property(e => e.Name)
          .HasMaxLength(25);
        entity.Property(a => a.Description)
          .HasMaxLength(500);

        entity.HasOne(c => c.Family)
          .WithMany()
          .HasForeignKey(c => c.FamilyId)
          .OnDelete(DeleteBehavior.Restrict);

        entity.HasData(new Category() { Id = 1, Name = "Frequent",SortOrder = 1, CategoryType = CatTypes.User, FamilyId = 1 },
          new Category() { Id = 2, Name = "Regular" , SortOrder = 2, CategoryType = CatTypes.User, FamilyId = 1 },
          new Category() { Id = 3, Name = "Infrequent", SortOrder = 3 , CategoryType = CatTypes.User, FamilyId = 1},
          new Category() { Id = 4, Name = "Income", SortOrder = 4, CategoryType = CatTypes.Income, FamilyId = 1},
          new Category() {Id = -1,Name = "System", SortOrder = 0, CategoryType = CatTypes.System, FamilyId = 1}
        );
      }
    }
  }


}