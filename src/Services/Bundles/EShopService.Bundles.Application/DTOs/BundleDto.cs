namespace EShopService.Bundles.Application.DTOs;

public sealed record BundleItemDto(
    Guid ItemId,
    string Name,
    string Description,
    string LicenseType,
    string FileFormat,
    long FileSizeBytes,
    bool IsReported,
    DateTime AddedAtUtc);

public sealed record BundleDto(
    Guid BundleId,
    Guid PrimaryArtistId,
    string Title,
    string Description,
    string PackLicenseType,
    decimal PersonalPrice,
    string Currency,
    decimal? CommercialPrice,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    IReadOnlyList<BundleItemDto> Items,
    IReadOnlyList<Guid> CoopArtists);