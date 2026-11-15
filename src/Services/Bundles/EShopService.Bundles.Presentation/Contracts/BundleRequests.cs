namespace EShopService.Bundles.Presentation.Contracts;

public sealed record CreateBundleRequest(
    Guid PrimaryArtistId,
    string Title,
    string PackLicenseType,
    decimal PersonalPrice = 10.00m,
    string Currency = "EUR",
    decimal? CommercialPrice = null);

public sealed record AddBundleItemRequest(
    Guid ItemId,
    string Name,
    string LicenseType);