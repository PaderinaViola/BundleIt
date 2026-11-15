namespace EShopService.Bundles.Domain.ValueObjects;

public readonly record struct BundleId(Guid Value)
{
    public static BundleId New() => new(Guid.NewGuid());
}

public readonly record struct BundleItemId(Guid Value)
{
    public static BundleItemId New() => new(Guid.NewGuid());
}

public readonly record struct ArtistId(Guid Value)
{
    public static ArtistId New() => new(Guid.NewGuid());
}