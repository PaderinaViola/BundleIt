namespace EShopService.Bundles.Presentation.Contracts;

public sealed record CreateBundleRequest(
    Guid PrimaryArtistId,
    string Title,
    string PackLicenseType,
    decimal PersonalPrice,
    string Currency,
    decimal? CommercialPrice = null,
    string Description = "Digital asset bundle");

public sealed record AddBundleItemRequest(
    Guid ItemId,
    string Name,
    string LicenseType,
    string Description = "Digital bundle asset",
    string FileFormat = "ZIP",
    long FileSizeBytes = 1024);