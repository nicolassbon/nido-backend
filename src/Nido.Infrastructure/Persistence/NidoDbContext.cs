using Microsoft.EntityFrameworkCore;
using Nido.Domain.Households;
using Nido.Infrastructure.Persistence.Configurations;

namespace Nido.Infrastructure.Persistence;

public class NidoDbContext : DbContext
{
    public NidoDbContext(DbContextOptions<NidoDbContext> options) : base(options)
    {
    }

    public DbSet<Household> Households => Set<Household>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new HouseholdConfiguration());
    }
}
