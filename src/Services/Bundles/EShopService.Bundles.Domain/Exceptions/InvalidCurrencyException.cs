namespace EShopService.Bundles.Domain.Exceptions;

public sealed class InvalidCurrencyException : DomainException
{
    public string AttemptedCurrency { get; }
    public InvalidCurrencyException(string currency)
        : base("bundle.invalid_currency", $"Currency '{currency}' is not supported.")
        => AttemptedCurrency = currency;
}