using Budget.Web.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Data
{
    public class IdentityDBContext(DbContextOptions<IdentityDBContext> options) : IdentityDbContext<BudgetUser>(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Use a dedicated schema for all ASP.NET Identity tables
            modelBuilder.HasDefaultSchema("BudgetIdentity");

            base.OnModelCreating(modelBuilder);

            // Ensure all Identity tables are created in the BudgetIdentity schema with default names
            modelBuilder.Entity<BudgetUser>().ToTable("AspNetUsers", "BudgetIdentity");
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles", "BudgetIdentity");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles", "BudgetIdentity");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims", "BudgetIdentity");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins", "BudgetIdentity");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims", "BudgetIdentity");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens", "BudgetIdentity");
        }
    }
}


