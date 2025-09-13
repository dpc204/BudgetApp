using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Budget.DB
{
  public class BudgetContext(DbContextOptions<BudgetContext> options) : DbContext(options)
  {
    public DbSet<Envelope> Envelopes { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<BankAccount> BankAccounts { get; set; }
    public DbSet<TransactionDetail> TransactionDetails { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // NEW: set default schema for this context
      modelBuilder.HasDefaultSchema("budget");

      modelBuilder.ApplyConfiguration(new User.UserConfiguration());
      modelBuilder.ApplyConfiguration(new Transaction.TransactionConfiguration());
      modelBuilder.ApplyConfiguration(new TransactionDetail.TransactionDetailConfiguration());
      modelBuilder.ApplyConfiguration(new Envelope.EnvelopeConfiguration());
      modelBuilder.ApplyConfiguration(new Category.CategoryConfiguration());
      modelBuilder.ApplyConfiguration(new BankAccount.BankAccountConfiguration());
    }
  }
}
