using System;

namespace EShopService.Bundles.Presentation.Contracts;

public sealed record PublishBundleRequest(Guid PrimaryArtistId, string Title, string PackLicenseType);
public sealed record AddBundleItemRequest(Guid ItemId, string Name, string LicenseType);