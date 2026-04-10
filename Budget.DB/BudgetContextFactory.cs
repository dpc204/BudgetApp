using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Budget.DB;

/// <summary>
/// Design-time factory for BudgetContext to support EF Core migrations
/// </summary>
public class BudgetContextFactory : IDesignTimeDbContextFactory<BudgetContext>
{
  public BudgetContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<BudgetContext>();

    // Use a default connection string for migrations
    optionsBuilder.UseSqlServer(
      "Data Source=(localdb)\\MSSQLLocalDB;Database=BudgetDB;Trusted_Connection=True;TrustServerCertificate=True",
      o => o.MigrationsHistoryTable("__EFMigrationsHistory", "budget"));

    return new BudgetContext(optionsBuilder.Options);
  }
}
