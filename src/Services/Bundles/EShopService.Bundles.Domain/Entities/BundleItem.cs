using EShopService.Bundles.Domain.Exceptions;
using System;

namespace EShopService.Bundles.Domain.Entities;

public sealed class BundleItem
{
    public Guid ItemId { get; }
    public string Name { get; }
    public string LicenseType { get; }
    public bool IsReported { get; private set; }

    internal BundleItem(Guid itemId, string name, string licenseType)
    {
        ItemId = itemId;
        Name = name;
        LicenseType = licenseType;
    }

    internal void MarkAsReported() => IsReported = true;
}