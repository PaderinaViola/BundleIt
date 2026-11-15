namespace EShopService.Bundles.Application.DTOs;

public sealed record BundleItemDto(Guid ItemId, string Name, string LicenseType, bool IsReported);

public sealed record BundleDto(
    Guid BundleId,
    Guid PrimaryArtistId,
    string Title,
    string PackLicenseType,
    decimal PersonalPrice,
    string Currency,
    decimal? CommercialPrice,
    string Status,
    IReadOnlyList<BundleItemDto> Items,
    IReadOnlyList<Guid> CoopArtists
);