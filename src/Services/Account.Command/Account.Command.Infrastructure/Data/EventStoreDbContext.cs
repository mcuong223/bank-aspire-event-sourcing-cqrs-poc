using Account.Command.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Account.Command.Infrastructure.Data;

public class EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : DbContext(options)
{
    public DbSet<EventEntity> Events { get; set; }
    public DbSet<SnapshotEntity> Snapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => new { e.AggregateId, e.Version })
            .IsUnique();
    }
}
