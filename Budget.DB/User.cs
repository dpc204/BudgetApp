using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.DB
{
  public class User
  {
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<Transaction> Transactions { get; set; } = [];

    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
      public void Configure(EntityTypeBuilder<User> entity)
      {
        entity.Property(u => u.FirstName)
          .HasMaxLength(50);
        entity.Property(u => u.LastName)
          .HasMaxLength(50);

        entity.HasData(new User {Id = 1,FirstName = "Patrick", LastName = "Connelly" },
          new User {Id=2, FirstName = "Terri", LastName = "Connelly" }
        );
      }
    }
  }
}