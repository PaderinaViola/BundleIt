using EShopService.Bundles.Domain.Entities;
using EShopService.Bundles.Domain.Exceptions;
using EShopService.Bundles.Domain.ValueObjects;

namespace EShopService.Bundles.Domain.Aggregates;

public sealed class Bundle
{
    private readonly List<BundleItem> _items = new();
    private readonly List<Guid> _coopArtistIds = new();

    public BundleId Id { get; private set; }
    public ArtistId PrimaryArtistId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public LicenseType PackLicenseType { get; private set; }
    public Money PersonalPrice { get; private set; } = default!;
    public Money? CommercialPrice { get; private set; }
    public BundleStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    public bool IsArchived => Status == BundleStatus.Archived;
    public IReadOnlyCollection<BundleItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<ArtistId> CoopArtists =>
        _coopArtistIds.Select(id => new ArtistId(id)).ToList().AsReadOnly();

    private Bundle()
    {
        Title = null!;
        Description = null!;
    }

    public Bundle(
        ArtistId primaryArtistId,
        string title,
        LicenseType packLicenseType,
        Money personalPrice,
        Money? commercialPrice = null,
        string description = "Digital asset bundle")
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.", nameof(description));

        // Invariant #12: Commercial price >= Personal price in the same currency
        if (commercialPrice is not null)
        {
            if (commercialPrice.Currency != personalPrice.Currency)
                throw new InvalidBundlePricingException("Commercial and Personal prices must use the same currency.");
            if (commercialPrice.Amount < personalPrice.Amount)
                throw new InvalidBundlePricingException("Commercial price cannot be lower than Personal price.");
        }

        Id = BundleId.New();
        PrimaryArtistId = primaryArtistId;
        Title = title.Trim();
        Description = description.Trim();
        PackLicenseType = packLicenseType;
        PersonalPrice = personalPrice;
        CommercialPrice = commercialPrice;
        Status = BundleStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
    }

    // Invariant #10: An archived bundle accepts no new items
    public void AddItem(
        BundleItemId itemId,
        string name,
        LicenseType license,
        string description = "Digital bundle asset",
        string fileFormat = "ZIP",
        long fileSizeBytes = 1024)
    {
        if (Status == BundleStatus.Archived)
            throw new BundleArchivedException(Id.Value);

        if (license != PackLicenseType)
            throw new InvalidLicenseException(license.ToString(), PackLicenseType.ToString());

        _items.Add(new BundleItem(itemId, name, license, description, fileFormat, fileSizeBytes));
    }

    // Invariant #11: A bundle can be published only if it contains at least one item
    public void Publish()
    {
        if (Status == BundleStatus.Archived)
            throw new BundleArchivedException(Id.Value);
        if (_items.Count == 0)
            throw new EmptyBundleException(Id.Value);

        Status = BundleStatus.Published;
        PublishedAtUtc ??= DateTime.UtcNow;
    }

    public void AddCoopArtist(ArtistId artistId)
    {
        if (Status == BundleStatus.Archived)
            throw new BundleArchivedException(Id.Value);

        if (artistId != PrimaryArtistId && !_coopArtistIds.Contains(artistId.Value))
            _coopArtistIds.Add(artistId.Value);
    }

    public void RemoveCoopArtist(ArtistId artistId)
    {
        if (Status == BundleStatus.Archived)
            throw new BundleArchivedException(Id.Value);

        _coopArtistIds.Remove(artistId.Value);
    }

    public void RemoveReportedItem(BundleItemId itemId)
    {
        var item = _items.FirstOrDefault(i => i.ItemId == itemId);
        item?.MarkAsReported();
    }

    public void Archive() => Status = BundleStatus.Archived;

    public BundleItem GetItem(BundleItemId itemId)
    {
        var item = _items.FirstOrDefault(i => i.ItemId == itemId);
        if (item is not null && item.IsReported)
            throw new ReportedItemException(itemId.Value);

        return item ?? throw new KeyNotFoundException($"Item '{itemId.Value}' was not found.");
    }
}