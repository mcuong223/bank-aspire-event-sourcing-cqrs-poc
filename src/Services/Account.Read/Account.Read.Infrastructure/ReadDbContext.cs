using Account.Read.Core;
using Microsoft.EntityFrameworkCore;

namespace Account.Read.Infrastructure;

public class ReadDbContext : DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options)
    {
    }

    public DbSet<AccountView> Accounts { get; set; }
}
