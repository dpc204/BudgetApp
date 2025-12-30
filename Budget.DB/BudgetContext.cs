using Microsoft.EntityFrameworkCore;

namespace Budget.DB
{
  public class BudgetContext(DbContextOptions<BudgetContext> options, ICurrentFamilyService? currentFamilyService = null) : DbContext(options)
  {
    private readonly ICurrentFamilyService? _currentFamilyService = currentFamilyService;

    public DbSet<Family> Families { get; set; }
    public DbSet<Envelope> Envelopes { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<BankAccount> BankAccounts { get; set; }
    public DbSet<TransactionDetail> TransactionDetails { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Favorite> Favorites { get; set; } // <-- add this
    public DbSet<BudgetMonth> BudgetMonths { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.HasDefaultSchema("budget");

      modelBuilder.ApplyConfiguration(new Family.FamilyConfiguration());
      modelBuilder.ApplyConfiguration(new User.UserConfiguration());
      modelBuilder.ApplyConfiguration(new Transaction.TransactionConfiguration());
      modelBuilder.ApplyConfiguration(new TransactionDetail.TransactionDetailConfiguration());
      modelBuilder.ApplyConfiguration(new Envelope.EnvelopeConfiguration());
      modelBuilder.ApplyConfiguration(new Category.CategoryConfiguration());
      modelBuilder.ApplyConfiguration(new BankAccount.BankAccountConfiguration());
      modelBuilder.ApplyConfiguration(new Favorite.FavoriteConfiguration()); // <-- add this
      modelBuilder.ApplyConfiguration(new BudgetMonth.BudgetMonthConfiguration());

      // Apply global query filters for multi-tenancy by FamilyId
      // Only filter when ICurrentFamilyService is available (not in migrations or seeding)
      if (_currentFamilyService != null)
      {
        var familyId = _currentFamilyService.GetCurrentFamilyId();
        
        modelBuilder.Entity<User>().HasQueryFilter(e => e.FamilyId == familyId);
        modelBuilder.Entity<BankAccount>().HasQueryFilter(e => e.FamilyId == familyId);
        modelBuilder.Entity<Category>().HasQueryFilter(e => e.FamilyId == familyId);
        modelBuilder.Entity<Envelope>().HasQueryFilter(e => e.FamilyId == familyId);
        modelBuilder.Entity<Transaction>().HasQueryFilter(e => e.FamilyId == familyId);
        modelBuilder.Entity<Favorite>().HasQueryFilter(e => e.FamilyId == familyId);
        modelBuilder.Entity<BudgetMonth>().HasQueryFilter(e => e.FamilyId == familyId);
      }

#if DEBUG
      var envelopeType = modelBuilder.Model.FindEntityType(typeof(Envelope));
      if (envelopeType != null)
      {
        var seeds = envelopeType.GetSeedData();
            Console.WriteLine($"DEBUG Envelope seed count = {seeds.Count()}");
        foreach (var row in seeds)
        {
          Console.WriteLine("DEBUG Seed -> " + string.Join(", ", row.Select(kv => kv.Key + "=" + kv.Value)));
        }
      }
#endif
    }
  }
}
