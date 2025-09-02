using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Budget.DB
{
  public class BudgetContext : DbContext
  {
    public DbSet<Envelope> Envelopes { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<BankAccount> BankAccounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=BudgetDb;Trusted_Connection=True;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.ApplyConfiguration(new User.UserConfiguration());
      modelBuilder.ApplyConfiguration(new Transaction.TransactionConfiguration());
      modelBuilder.ApplyConfiguration(new TransactionDetail.TransactionDetailConfiguration());
      modelBuilder.ApplyConfiguration(new Envelope.EnvelopeConfiguration());
      modelBuilder.ApplyConfiguration(new BankAccount.BankAccountConfiguration());
    }
  }
}
