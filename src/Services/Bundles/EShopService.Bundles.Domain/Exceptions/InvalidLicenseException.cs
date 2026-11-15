namespace EShopService.Bundles.Domain.Exceptions;

public sealed class InvalidLicenseException : DomainException
{
    public string AttemptedLicense { get; }
    public string RequiredLicense { get; }

    public InvalidLicenseException(string attemptedLicense, string requiredLicense)
        : base("bundle.invalid_license", $"Cannot issue '{attemptedLicense}' license. This pack requires a '{requiredLicense}' license.")
    {
        AttemptedLicense = attemptedLicense;
        RequiredLicense = requiredLicense;
    }
}