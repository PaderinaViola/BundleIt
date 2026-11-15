using EShopService.Bundles.Domain.Aggregates;
using EShopService.Bundles.Domain.ValueObjects;

namespace EShopService.Bundles.Application.Repositories;

public interface IBundleRepository
{
    Task<Bundle?> GetByIdAsync(BundleId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bundle>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(Bundle bundle, CancellationToken cancellationToken = default);
    Task DeleteAsync(Bundle bundle, CancellationToken cancellationToken = default);
}