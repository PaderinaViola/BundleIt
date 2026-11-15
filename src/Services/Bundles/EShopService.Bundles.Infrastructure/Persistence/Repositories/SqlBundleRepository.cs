using EShopService.Bundles.Application.Repositories;
using EShopService.Bundles.Domain.Aggregates;
using EShopService.Bundles.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EShopService.Bundles.Infrastructure.Persistence.Repositories;

internal sealed class SqlBundleRepository : IBundleRepository
{
    private readonly BundlesDbContext _dbContext;

    public SqlBundleRepository(BundlesDbContext dbContext) => _dbContext = dbContext;

    public async Task<Bundle?> GetByIdAsync(BundleId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bundles
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Bundle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bundles
            .AsNoTracking()
            .Include(b => b.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(Bundle bundle, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(bundle).State == EntityState.Detached)
        {
            await _dbContext.Bundles.AddAsync(bundle, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Bundle bundle, CancellationToken cancellationToken = default)
    {
        _dbContext.Bundles.Remove(bundle);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}