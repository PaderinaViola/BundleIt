namespace EShopService.Bundles.Domain.Exceptions;

public sealed class InvalidPriceException : DomainException
{
    public decimal AttemptedPrice { get; }
    public InvalidPriceException(decimal attemptedPrice)
        : base("bundle.invalid_price", $"Price cannot be negative: {attemptedPrice}.")
        => AttemptedPrice = attemptedPrice;
}