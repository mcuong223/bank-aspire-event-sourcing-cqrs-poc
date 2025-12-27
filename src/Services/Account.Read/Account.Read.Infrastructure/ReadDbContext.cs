using Account.Read.Core;
using Microsoft.EntityFrameworkCore;

namespace Account.Read.Infrastructure;

public class ReadDbContext : DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options)
    {
    }

    public DbSet<AccountView> Accounts { get; set; }
    public DbSet<TransactionHistoryView> TransactionHistory { get; set; }
    public DbSet<LoyaltyView> LoyaltyScores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoyaltyView>().HasKey(l => l.AccountId);
        base.OnModelCreating(modelBuilder);
    }
}
