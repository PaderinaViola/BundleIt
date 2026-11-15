using EShopService.Bundles.Application.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EShopService.Bundles.Application.Services;

public interface IBundleService
{
    Task<BundleDto> PublishBundleAsync(Guid primaryArtistId, string title, string packLicenseType, CancellationToken cancellationToken = default);
    Task<BundleDto?> GetBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);
    Task<BundleDto> AddItemAsync(Guid bundleId, Guid itemId, string name, string licenseType, CancellationToken cancellationToken = default);
    Task<BundleDto> ArchiveBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);
}