namespace EShopService.Bundles.Domain.Exceptions;

public sealed class InvalidBundlePricingException : DomainException
{
    public InvalidBundlePricingException(string message)
        : base("bundle.invalid_pricing", message) { }
}