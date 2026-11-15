using System;
using System.Collections.Generic;

namespace EShopService.Bundles.Application.DTOs;

public sealed record BundleItemDto(
    Guid ItemId,
    string Name,
    string LicenseType,
    bool IsReported
);

public sealed record BundleDto(
    Guid BundleId,
    Guid PrimaryArtistId,
    string Title,
    string PackLicenseType,
    bool IsArchived,
    IReadOnlyList<BundleItemDto> Items,
    IReadOnlyList<Guid> CoopArtists
);