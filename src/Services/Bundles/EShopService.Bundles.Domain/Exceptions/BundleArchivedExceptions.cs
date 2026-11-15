namespace EShopService.Bundles.Domain.Exceptions;

public sealed class BundleArchivedException : DomainException
{
    public Guid BundleId { get; }

    public BundleArchivedException(Guid bundleId)
        : base("bundle.archived", $"Cannot modify bundle '{bundleId}' because it is archived and closed to new items.")
    {
        BundleId = bundleId;
    }
}