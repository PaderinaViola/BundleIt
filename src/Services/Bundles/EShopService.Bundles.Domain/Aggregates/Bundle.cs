using EShopService.Bundles.Domain.Entities;
using EShopService.Bundles.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EShopService.Bundles.Domain.Aggregates;

public sealed class Bundle
{
    private readonly List<BundleItem> _items = new();
    private readonly List<Guid> _coopArtists = new();

    public Guid Id { get; } = Guid.NewGuid();
    public Guid PrimaryArtistId { get; }
    public string Title { get; }
    public string PackLicenseType { get; }
    public bool IsArchived { get; private set; }

    public IReadOnlyCollection<BundleItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<Guid> CoopArtists => _coopArtists.AsReadOnly();

    public Bundle(Guid primaryArtistId, string title, string packLicenseType)
    {
        PrimaryArtistId = primaryArtistId;
        Title = title;
        PackLicenseType = packLicenseType;
    }

    public void AddItem(Guid itemId, string name, string licenseType)
    {
        if (IsArchived)
            throw new BundleArchivedException(Id);

        if (!string.Equals(licenseType, PackLicenseType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidLicenseException(licenseType, PackLicenseType);

        _items.Add(new BundleItem(itemId, name, licenseType));
    }

    public void AddCoopArtist(Guid artistId)
    {
        if (IsArchived)
            throw new BundleArchivedException(Id);

        if (!_coopArtists.Contains(artistId))
            _coopArtists.Add(artistId);
    }

    public void RemoveReportedItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.ItemId == itemId);
        item?.MarkAsReported();
    }

    public void Archive() => IsArchived = true;

    public BundleItem GetItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.ItemId == itemId);
        if (item != null && item.IsReported)
            throw new ReportedItemException(itemId);

        return item ?? throw new Exception("Item not found.");
    }
}