using EShopService.Bundles.Application.DTOs;
using EShopService.Bundles.Application.Repositories;
using EShopService.Bundles.Domain.Aggregates;
using EShopService.Bundles.Domain.ValueObjects;

namespace EShopService.Bundles.Application.Services;

public interface IBundleService
{
    Task<BundleDto> CreateBundleAsync(Guid primaryArtistId, string title, string packLicenseType, decimal personalPrice, string currency, decimal? commercialPrice = null, CancellationToken cancellationToken = default);
    Task<BundleDto?> GetBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BundleDto>> GetAllBundlesAsync(CancellationToken cancellationToken = default);
    Task<BundleDto> AddItemAsync(Guid bundleId, Guid itemId, string name, string licenseType, CancellationToken cancellationToken = default);
    Task<BundleDto> PublishBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);
    Task<BundleDto> ArchiveBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);
    Task DeleteBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);
}

public sealed class BundleService : IBundleService
{
    private readonly IBundleRepository _bundleRepository;

    public BundleService(IBundleRepository bundleRepository) => _bundleRepository = bundleRepository;

    public async Task<BundleDto> CreateBundleAsync(
        Guid primaryArtistId, string title, string packLicenseType,
        decimal personalPrice, string currency, decimal? commercialPrice = null,
        CancellationToken cancellationToken = default)
    {
        var license = Enum.Parse<LicenseType>(packLicenseType, ignoreCase: true);
        var personalMoney = Money.Of(personalPrice, currency);
        var commercialMoney = commercialPrice.HasValue ? Money.Of(commercialPrice.Value, currency) : null;

        var bundle = new Bundle(new ArtistId(primaryArtistId), title, license, personalMoney, commercialMoney);
        await _bundleRepository.SaveAsync(bundle, cancellationToken);
        return MapToDto(bundle);
    }

    public async Task<BundleDto?> GetBundleAsync(Guid bundleId, CancellationToken cancellationToken = default)
    {
        var bundle = await _bundleRepository.GetByIdAsync(new BundleId(bundleId), cancellationToken);
        return bundle is null ? null : MapToDto(bundle);
    }

    public async Task<IReadOnlyList<BundleDto>> GetAllBundlesAsync(CancellationToken cancellationToken = default)
    {
        var bundles = await _bundleRepository.GetAllAsync(cancellationToken);
        return bundles.Select(MapToDto).ToList();
    }

    public async Task<BundleDto> AddItemAsync(Guid bundleId, Guid itemId, string name, string licenseType, CancellationToken cancellationToken = default)
    {
        var bundle = await GetExistingOrThrowAsync(bundleId, cancellationToken);
        var license = Enum.Parse<LicenseType>(licenseType, ignoreCase: true);
        bundle.AddItem(new BundleItemId(itemId), name, license);
        await _bundleRepository.SaveAsync(bundle, cancellationToken);
        return MapToDto(bundle);
    }

    public async Task<BundleDto> PublishBundleAsync(Guid bundleId, CancellationToken cancellationToken = default)
    {
        var bundle = await GetExistingOrThrowAsync(bundleId, cancellationToken);
        bundle.Publish();
        await _bundleRepository.SaveAsync(bundle, cancellationToken);
        return MapToDto(bundle);
    }

    public async Task<BundleDto> ArchiveBundleAsync(Guid bundleId, CancellationToken cancellationToken = default)
    {
        var bundle = await GetExistingOrThrowAsync(bundleId, cancellationToken);
        bundle.Archive();
        await _bundleRepository.SaveAsync(bundle, cancellationToken);
        return MapToDto(bundle);
    }

    public async Task DeleteBundleAsync(Guid bundleId, CancellationToken cancellationToken = default)
    {
        var bundle = await GetExistingOrThrowAsync(bundleId, cancellationToken);
        await _bundleRepository.DeleteAsync(bundle, cancellationToken);
    }

    private async Task<Bundle> GetExistingOrThrowAsync(Guid bundleId, CancellationToken cancellationToken) =>
        await _bundleRepository.GetByIdAsync(new BundleId(bundleId), cancellationToken)
        ?? throw new KeyNotFoundException($"Bundle '{bundleId}' was not found.");

    private static BundleDto MapToDto(Bundle b) => new(
        b.Id.Value,
        b.PrimaryArtistId.Value,
        b.Title,
        b.PackLicenseType.ToString(),
        b.PersonalPrice.Amount,
        b.PersonalPrice.Currency,
        b.CommercialPrice?.Amount,
        b.Status.ToString(),
        b.Items.Select(i => new BundleItemDto(i.ItemId.Value, i.Name, i.License.ToString(), i.IsReported)).ToList(),
        b.CoopArtists.Select(a => a.Value).ToList()
    );
}