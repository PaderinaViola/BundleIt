using EShopService.Bundles.Application.DTOs;
using EShopService.Bundles.Application.Repositories;
using EShopService.Bundles.Domain.Aggregates;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EShopService.Bundles.Application.Services;

public sealed class BundleService : IBundleService
{
    private readonly IBundleRepository _bundleRepository;

    public BundleService(IBundleRepository bundleRepository)
    {
        _bundleRepository = bundleRepository;
    }

    public async Task<BundleDto> PublishBundleAsync(Guid primaryArtistId, string title, string packLicenseType, CancellationToken cancellationToken = default)
    {
        var bundle = new Bundle(primaryArtistId, title, packLicenseType);
        await _bundleRepository.SaveAsync(bundle, cancellationToken);
        return MapToDto(bundle);
    }

    public async Task<BundleDto?> GetBundleAsync(Guid bundleId, CancellationToken cancellationToken = default)
    {
        var bundle = await _bundleRepository.GetByIdAsync(bundleId, cancellationToken);
        return bundle is null ? null : MapToDto(bundle);
    }

    public async Task<BundleDto> AddItemAsync(Guid bundleId, Guid itemId, string name, string licenseType, CancellationToken cancellationToken = default)
    {
        var bundle = await GetExistingBundleOrThrowAsync(bundleId, cancellationToken);
        bundle.AddItem(itemId, name, licenseType);
        await _bundleRepository.SaveAsync(bundle, cancellationToken);
        return MapToDto(bundle);
    }

    public async Task<BundleDto> ArchiveBundleAsync(Guid bundleId, CancellationToken cancellationToken = default)
    {
        var bundle = await GetExistingBundleOrThrowAsync(bundleId, cancellationToken);
        bundle.Archive();
        await _bundleRepository.SaveAsync(bundle, cancellationToken);
        return MapToDto(bundle);
    }

    private async Task<Bundle> GetExistingBundleOrThrowAsync(Guid bundleId, CancellationToken cancellationToken)
    {
        var bundle = await _bundleRepository.GetByIdAsync(bundleId, cancellationToken);
        return bundle ?? throw new Exception($"Bundle with ID '{bundleId}' was not found.");
    }

    private static BundleDto MapToDto(Bundle bundle) =>
        new(
            bundle.Id,
            bundle.PrimaryArtistId,
            bundle.Title,
            bundle.PackLicenseType,
            bundle.IsArchived,
            bundle.Items.Select(i => new BundleItemDto(i.ItemId, i.Name, i.LicenseType, i.IsReported)).ToList(),
            bundle.CoopArtists.ToList()
        );
}