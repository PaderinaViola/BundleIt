using EShopService.Bundles.Domain.ValueObjects;

namespace EShopService.Bundles.Domain.Entities;

public sealed class BundleItem
{
    public BundleItemId ItemId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public LicenseType License { get; private set; }
    public string FileFormat { get; private set; }
    public long FileSizeBytes { get; private set; }
    public bool IsReported { get; private set; }
    public DateTime AddedAtUtc { get; private set; }

    private BundleItem()
    {
        Name = null!;
        Description = null!;
        FileFormat = null!;
    }

    internal BundleItem(
        BundleItemId itemId,
        string name,
        LicenseType license,
        string description = "Digital bundle asset",
        string fileFormat = "ZIP",
        long fileSizeBytes = 1024)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Item description cannot be empty.", nameof(description));
        if (string.IsNullOrWhiteSpace(fileFormat))
            throw new ArgumentException("File format cannot be empty.", nameof(fileFormat));
        if (fileSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be greater than zero.");

        ItemId = itemId;
        Name = name.Trim();
        Description = description.Trim();
        License = license;
        FileFormat = fileFormat.Trim().ToUpperInvariant();
        FileSizeBytes = fileSizeBytes;
        AddedAtUtc = DateTime.UtcNow;
    }

    internal void MarkAsReported() => IsReported = true;
}