using EShopService.Bundles.Domain.Aggregates;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EShopService.Bundles.Application.Repositories;

public interface IBundleRepository
{
    Task<Bundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(Bundle bundle, CancellationToken cancellationToken = default);
}