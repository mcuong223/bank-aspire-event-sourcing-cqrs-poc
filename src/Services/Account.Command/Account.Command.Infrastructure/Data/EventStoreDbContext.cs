using Account.Command.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Account.Command.Infrastructure.Data;

public class EventStoreDbContext : DbContext
{
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : base(options)
    {
    }

    public DbSet<EventEntity> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => new { e.AggregateId, e.Version })
            .IsUnique();
    }
}
