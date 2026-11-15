using EShopService.Bundles.Application.Repositories;
using EShopService.Bundles.Domain.Aggregates;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EShopService.Bundles.Presentation.Infrastructure;

public sealed class InMemoryBundleRepository : IBundleRepository
{
    private readonly ConcurrentDictionary<Guid, Bundle> _storage = new();

    public Task<Bundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(id, out var bundle);
        return Task.FromResult(bundle);
    }

    public Task SaveAsync(Bundle bundle, CancellationToken cancellationToken = default)
    {
        _storage[bundle.Id] = bundle;
        return Task.CompletedTask;
    }
}