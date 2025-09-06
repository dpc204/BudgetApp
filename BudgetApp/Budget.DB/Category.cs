using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Budget.DB
{
  public class Category
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Envelope> Envelopes { get; set; } = new();

    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
      public void Configure(EntityTypeBuilder<Category> entity)
      {
        entity.Property(e => e.Name)
          .HasMaxLength(25);
        entity.Property(a => a.Description)
          .HasMaxLength(500);

        entity.HasData(new Category() { Id = 1, Name = "Frequent", },
          new Category() { Id = 2, Name = "Regular" }
        );
      }
    }
  }
}