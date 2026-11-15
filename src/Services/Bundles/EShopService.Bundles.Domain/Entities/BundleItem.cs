using EShopService.Bundles.Domain.Exceptions;
using System;
using EShopService.Bundles.Domain.ValueObjects;


namespace EShopService.Bundles.Domain.Entities;

public sealed class BundleItem
{
    public BundleItemId ItemId { get; private set; }
    public string Name { get; private set; }
    public LicenseType License { get; private set; }
    public bool IsReported { get; private set; }

    private BundleItem() => Name = null!; // For EF Core

    internal BundleItem(BundleItemId itemId, string name, LicenseType license)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name cannot be empty.", nameof(name));

        ItemId = itemId;
        Name = name.Trim();
        License = license;
    }

    internal void MarkAsReported() => IsReported = true;
}