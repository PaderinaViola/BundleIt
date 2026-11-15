namespace EShopService.Bundles.Domain.Exceptions;

public sealed class EmptyBundleException : DomainException
{
    public Guid BundleId { get; }
    public EmptyBundleException(Guid bundleId)
        : base("bundle.empty", $"Bundle '{bundleId}' cannot be published without at least one item.")
        => BundleId = bundleId;
}