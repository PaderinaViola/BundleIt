using EShopService.Bundles.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace EShopService.Bundles.Infrastructure.Persistence;

internal sealed class BundlesDbContext : DbContext
{
    public BundlesDbContext(DbContextOptions<BundlesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Bundle> Bundles => Set<Bundle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BundlesDbContext).Assembly);
    }
}