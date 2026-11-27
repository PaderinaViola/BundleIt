# Aggregates and Invariants: Value Objects and Strongly Typed Identifiers

**Project:** BundleIt (working name), a worldwide platform where creators sell themed bundles of digital items (illustrations, brush sets, tutorials, music samples...) with a one-time payment.
**Source:** Event Storming board and the purchase BPMN model.
**Related document:** [`stateDiagram-purchase.md`](./stateDiagram-purchase.md) (state machine, invariants, Given-When-Then scenarios, design patterns).

## 1. Ubiquitous Language (short glossary)

| Term | Meaning in the project |
|---|---|
| **Artist** | Creator who publishes bundles and receives payouts. |
| **Buyer** | Person or company, from any country, who buys bundles. |
| **Bundle** | Themed collection ("playlist") of digital items, e.g. *Inktober 2026*. It can keep growing or be archived. |
| **Series** | Related bundles, e.g. *Part 5*, *Part 6*. Each part is a separate bundle with its own purchase. |
| **Cart** | The buyer's basket. It holds up to **25 items** in total before checkout. |
| **Cart line** | One bundle with one license type and a quantity inside a cart. |
| **Purchase** | One checked-out cart line: one buyer, one bundle, one license type, one quantity. The pivotal aggregate. |
| **License type** | **Personal use** (view-only access) or **Commercial** (view access plus the right to download the original files). |
| **View access** | Right to open the bundle's items inside the app. Every paying buyer has it. |
| **Download license** | Right to download the original files. Only a Commercial license includes it. |
| **Usage** | What the buyer did with a bundle: *opened* (viewed) an item or *downloaded* a file. Used to decide refunds. |
| **Archived bundle** | Bundle that accepts no new items. Existing buyers keep access, and new buyers can still buy it. |
| **Payout** | Transfer of the artist's earnings to the artist. |

## 2. Why primitive types are dangerous in this system

- **Swapped `Guid`s:** `StartPurchase(bundleId, buyerId)` instead of `(buyerId, bundleId)` compiles and stores a "ghost purchase" that no buyer ever sees.
- **Blind `decimal`:** a bundle priced `20.00 EUR` plus `10.00 PLN` gives `30.00`. In a worldwide shop this happens in the cart total on day one. Also, `100 JPY` has no decimals while `100 KWD` has three.
- **Unchecked `int`:** `quantity = -3` lowers the cart total, and nothing stops a cart with 4,000 items.
- **Unchecked `string`:** `taxId = "none"` on a commercial purchase gets the invoice rejected, and `fileName = "../../etc/passwd"` is a security problem.

## 3. Value Objects (replacing raw `decimal`, `string`, `int` and date pairs)

All Value Objects are `sealed record` types: immutable, compared by value, and self-validating in the factory method. Invalid input throws a domain exception and no object is created.

| # | Value Object | Replaces / used in | Components (type) | Self-validation (rejected input) | Example operation returning a new instance |
|---|---|---|---|---|---|
| 1 | **`Money`** | raw `decimal` prices, refunds, earnings (Catalog, Cart, Payments, Payouts) | `Amount` (`decimal`), `Currency` (`string`, ISO 4217 code) | `Amount < 0` rejected (`InvalidPriceException`). `Currency` must be in the **supported-currency catalog** (e.g. `PLN, EUR, USD, GBP, CHF, JPY`), otherwise `InvalidCurrencyException`. `Amount` is rounded to the **minor unit of that currency** (2 decimals for `EUR`, 0 for `JPY`) with `MidpointRounding.ToEven`. Adding or subtracting different currencies rejected (`CurrencyMismatchException`). A subtraction with a negative result is rejected. | `Money.Of(60m,"EUR").Add(Money.Of(20m,"EUR"))` returns a **new** `Money(80.00, EUR)`. Both operands are unchanged. Also `Multiply(int)` and `Percentage(decimal)`. |
| 2 | **`Quantity`** | raw `int` item counts on cart lines and purchases (Cart, Checkout) | `Value` (`int`) | `Value < 1` or `Value > 25` rejected (`InvalidQuantityException`). Adding two quantities whose sum is above 25 is rejected. | `Quantity.Of(2).Add(Quantity.Of(3))` returns a **new** `Quantity(5)`. The cart-wide limit (25 items across all lines) is guarded by the `Cart` aggregate. |
| 3 | **`TaxId`** | raw `string nip` / `vatNumber` for business buyers from any country (Checkout, Invoices) | `CountryCode` (`string`, ISO 3166-1 alpha-2), `Number` (`string`, normalized) | `CountryCode` must be a supported country code. In `Number`, spaces, hyphens and dots are stripped, letters are upper-cased, and a leading country prefix is removed (`"DE 123 456 789"` with `DE` becomes `"123456789"`). **Generic rule for every country:** 4..20 letters or digits. **Country rule, where one is registered:** `PL` = 10 digits with the weighted modulo-11 checksum, `DE` = 9 digits, `US` (EIN) = 9 digits. A country without a specific rule only gets the generic rule. `"none"`, empty text and symbols are rejected (`InvalidTaxIdException`). | `TaxId.Parse("PL", " 123-456-32-18 ")` returns a **new** normalized `TaxId("PL","1234563218")`. A `TaxId` has no mutating methods. To correct it, parse a new one and replace it through `BillingDetails.ChangeTaxId(...)`. |
| 4 | **`BillingDetails`** | loose `string companyName`, `taxId`, `street`, `city`... for Commercial buyers (Checkout, Invoices) | `CompanyName`, `TaxId`, `AddressLine`, `PostalCode`, `City`, `CountryCode` | `CompanyName` blank or outside 2..200 characters rejected. `AddressLine` and `City` must not be blank. `PostalCode` must be 2..12 characters (letters, digits, space, hyphen). Where a country has a registered format it is also checked (e.g. `PL` `NN-NNN`, `US` `NNNNN` or `NNNNN-NNNN`). `CountryCode` must be a supported ISO alpha-2 code and **must equal `TaxId.CountryCode`**. All violations throw `InvalidBillingDetailsException`. | `details.ChangeAddress(addressLine, postalCode, city)` returns a **new** `BillingDetails`. The old object, which may be referenced by an issued invoice, stays untouched. |
| 5 | **`PaymentDeadline`** | pair of raw `DateTime startedAt, expiresAt` (Checkout, Payments) | `StartsAt` (`DateTimeOffset`), `ExpiresAt` (`DateTimeOffset`) | `ExpiresAt` must be after `StartsAt`. Duration must be between **15 minutes and 24 hours**. Both values must be UTC (offset 0) (`InvalidPaymentDeadlineException`). | `deadline.Extend(TimeSpan.FromMinutes(30))` returns a **new** deadline. If the total duration would exceed 24 h it throws `InvalidPaymentDeadlineException`. `IsExpiredAt(now)` is a pure query. |
| 6 | **`UsageSummary`** | raw `int viewCount`, `bool wasDownloaded` flags passed to the refund decision (Access, Refunds) | `ViewCount` (`int`), `DownloadCount` (`int`) | Either count `< 0` rejected (`InvalidUsageSummaryException`). | `usage.RecordView()` returns a **new** summary with `ViewCount + 1`. `RecordDownload()` does the same for `DownloadCount`. Derived queries: `WasOpened` (`ViewCount > 0`) and `WasDownloaded` (`DownloadCount > 0`). |
| 7 | **`SeriesPart`** | raw `int partNumber` (Catalog) | `Number` (`int`) | `Number < 1` or `> 999` rejected (`InvalidSeriesPartException`). | `SeriesPart.Of(5).Next()` returns a **new** `SeriesPart(6)`, displayed as "Part 6". |
| 8 | **`ArtFile`** | raw `string fileName`, `long size`, `string hash` (Catalog, Delivery) | `FileName` (`string`), `Format` (`string`), `SizeBytes` (`long`), `Sha256` (`string`) | `FileName` blank, longer than 120 characters or containing `/` or `\` rejected. `Format` must be one of `png, jpg, psd, clip, procreate, abr, brushset, zip, pdf, mp3, wav, mp4`. `SizeBytes` must be **at least 1 KB (1,024 bytes) and at most 2 GB (2,147,483,648 bytes)**, so a 500 KB file is valid. `Sha256` must be exactly 64 hex characters (`InvalidArtFileException`). | `file.Rename("ink-day-05.png")` returns a **new** `ArtFile` with the same checksum. The extension must stay the same, otherwise `InvalidArtFileException`. |

`LicenseType` (`Personal`, `Commercial`) is a plain `enum` and not a Value Object, because it is a closed set of two values with no further attributes. `Personal` means view-only access and `Commercial` means view plus download.

**Deliberately not used:** `ShippingAddress` (digital goods are not shipped; the address lives in `BillingDetails` and is only needed for Commercial purchases).

### Reference contracts (C#)

```csharp
namespace BundleIt.SharedKernel.ValueObjects;

public static class CurrencyCatalog
{
    // ISO 4217 minor units: number of decimals of the smallest currency unit
    private static readonly IReadOnlyDictionary<string, int> MinorUnits = new Dictionary<string, int>
    {
        ["PLN"] = 2, ["EUR"] = 2, ["USD"] = 2, ["GBP"] = 2, ["CHF"] = 2, ["JPY"] = 0
    };

    public static bool TryGetMinorUnits(string code, out int digits) => MinorUnits.TryGetValue(code, out digits);
}

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

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
            throw new InvalidCurrencyException(currency);

        return new Money(amount, code, minorUnits);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return Of(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return Of(Amount - other.Amount, Currency);   // a negative result is rejected
    }

    public Money Multiply(int factor) => Of(Amount * factor, Currency);
    public Money Percentage(decimal rate) => Of(Amount * rate, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new CurrencyMismatchException(Currency, other.Currency);
    }
}

public sealed record Quantity
{
    public const int Max = 25;
    public int Value { get; }

    private Quantity(int value) => Value = value;

    public static Quantity Of(int value) =>
        value is < 1 or > Max ? throw new InvalidQuantityException(value) : new Quantity(value);

    public Quantity Add(Quantity other) => Of(Value + other.Value);
}

public sealed record UsageSummary(int ViewCount, int DownloadCount)
{
    public static UsageSummary None { get; } = new(0, 0);

    public bool WasOpened => ViewCount > 0;
    public bool WasDownloaded => DownloadCount > 0;

    public UsageSummary RecordView() => new(ViewCount + 1, DownloadCount);
    public UsageSummary RecordDownload() => new(ViewCount, DownloadCount + 1);
}

// Country-specific tax id formats are pluggable (Strategy), so supporting a new country
// means adding a class, not editing TaxId (Open/Closed Principle).
public interface ITaxIdFormatRule
{
    string CountryCode { get; }
    bool IsValid(string normalizedNumber);
}

public sealed class PolishNipRule : ITaxIdFormatRule
{
    private static readonly int[] Weights = { 6, 5, 7, 2, 3, 4, 5, 6, 7 };
    public string CountryCode => "PL";

    public bool IsValid(string n)
    {
        if (n.Length != 10 || !n.All(char.IsDigit)) return false;
        var sum = Weights.Select((w, i) => w * (n[i] - '0')).Sum() % 11;
        return sum != 10 && sum == n[9] - '0';
    }
}
```

## 4. Strongly typed identifiers

Each identifier is a `readonly record struct` wrapping a `Guid`. The compiler then rejects a swapped call such as `StartPurchase(bundleId, buyerId)`.

```csharp
public readonly record struct BundleId(Guid Value)
{
    public static BundleId New() => new(Guid.NewGuid());
}
```

| Identifier | Identifies | Aggregate / role |
|---|---|---|
| `ArtistId` | Artist | Root of the Artist (profile) aggregate |
| `BuyerId` | Buyer (private person or company) | Root of the Buyer aggregate |
| `BundleId` | Bundle | **Aggregate root** (Catalog & Bundles) |
| `BundleItemId` | Single item inside a bundle | Internal entity of `Bundle` |
| `SeriesId` | Series of bundles (Part 5, Part 6...) | Reference by id from `Bundle` |
| `CartId` | Cart | **Aggregate root** (Cart & Checkout) |
| `CartLineId` | One line of a cart | Internal entity of `Cart` |
| `PurchaseId` | Purchase (one checked-out cart line) | **Aggregate root** (Purchases) |
| `PaymentId` | Payment (one payment can cover several purchases) | **Aggregate root** (Payments) |
| `RefundId` | Refund | Entity linked to `Purchase` and `Payment` by id |
| `AccessRightId` | View access and download license of one purchase | **Aggregate root** (Access & Licensing) |
| `UsageRecordId` | Usage history (opens and downloads) of one purchase | **Aggregate root** (Access & Licensing), source of `UsageSummary` |
| `PayoutId` | Payout to an artist | **Aggregate root** (Artist Payouts) |
| `InvoiceId` | Invoice for a Commercial purchase | **Aggregate root** (Invoices) |
| `ReportId` | Report of an artwork (fan art, copyright complaint) | Root of the Report aggregate |

Aggregates refer to each other **only by these identifiers**. For example, `Purchase` stores `BuyerId`, `BundleId` and a `Money PriceSnapshot` instead of holding a `Bundle` object, and receives usage data as a `UsageSummary` value.

## 5. Entity or Value Object? (decision summary)

| Concept | Modeled as | Reason |
|---|---|---|
| Artist, Buyer, Bundle, BundleItem, Cart, CartLine, Purchase, Payment, AccessRight, UsageRecord, Payout, Invoice | **Entity** | Has an identity and a life cycle. Its attributes change while it stays the same object (a cart gains lines, a purchase goes from pending to refunded). |
| Money, Quantity, TaxId, BillingDetails, PaymentDeadline, UsageSummary, SeriesPart, ArtFile | **Value Object** | Described only by its values, immutable, interchangeable. A change means a replacement. |
