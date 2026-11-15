using EShopService.Bundles.Domain.Exceptions;

namespace EShopService.Bundles.Domain.ValueObjects;

public static class CurrencyCatalog
{
    private static readonly IReadOnlyDictionary<string, int> MinorUnits = new Dictionary<string, int>
    {
        ["UAH"] = 2,
        ["PLN"] = 2,
        ["EUR"] = 2,
        ["USD"] = 2,
        ["GBP"] = 2,
        ["CHF"] = 2,
        ["JPY"] = 0
    };

    public static bool TryGetMinorUnits(string code, out int digits) =>
        MinorUnits.TryGetValue(code, out digits);
}

public sealed record Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    private Money() => Currency = null!; // For EF Core materialization

    private Money(decimal amount, string currency, int minorUnits)
    {
        Amount = decimal.Round(amount, minorUnits, MidpointRounding.ToEven);
        Currency = currency;
    }

    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0m)
            throw new InvalidPriceException(amount);

        var code = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (!CurrencyCatalog.TryGetMinorUnits(code, out var minorUnits))
            throw new InvalidCurrencyException(currency ?? string.Empty);

        return new Money(amount, code, minorUnits);
    }
}