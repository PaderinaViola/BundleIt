using EShopService.Bundles.Domain.Aggregates;
using EShopService.Bundles.Domain.Exceptions;
using EShopService.Bundles.Domain.ValueObjects;
using Xunit;

namespace EShopService.Bundles.Domain.Tests;

public sealed class BundleTests
{
    [Fact]
    public void AddItem_WithMatchingLicense_AddsItemToBundle()
    {
        var bundle = new Bundle(ArtistId.New(), "Inktober 2026", LicenseType.Commercial, Money.Of(20m, "EUR"));
        var itemId = BundleItemId.New();

        bundle.AddItem(itemId, "Brush Pack", LicenseType.Commercial);

        Assert.Single(bundle.Items);
        Assert.Equal(itemId, bundle.Items.First().ItemId);
    }

    [Fact]
    public void AddItem_WithMismatchedLicense_ThrowsInvalidLicenseException()
    {
        var bundle = new Bundle(ArtistId.New(), "Inktober 2026", LicenseType.Commercial, Money.Of(20m, "EUR"));

        Assert.Throws<InvalidLicenseException>(() =>
            bundle.AddItem(BundleItemId.New(), "Personal Sketch", LicenseType.Personal));
    }

    [Fact]
    public void AddItem_WhenArchived_ThrowsBundleArchivedException()
    {
        var bundle = new Bundle(ArtistId.New(), "Inktober 2026", LicenseType.Personal, Money.Of(15m, "EUR"));
        bundle.Archive();

        Assert.Throws<BundleArchivedException>(() =>
            bundle.AddItem(BundleItemId.New(), "New Sketch", LicenseType.Personal));
    }

    [Fact]
    public void Publish_WhenEmpty_ThrowsEmptyBundleException()
    {
        var bundle = new Bundle(ArtistId.New(), "Empty Pack", LicenseType.Personal, Money.Of(15m, "EUR"));

        Assert.Throws<EmptyBundleException>(() => bundle.Publish());
    }

    [Fact]
    public void Constructor_WhenCommercialPriceLowerThanPersonal_ThrowsInvalidBundlePricingException()
    {
        Assert.Throws<InvalidBundlePricingException>(() =>
            new Bundle(ArtistId.New(), "Bad Pricing", LicenseType.Commercial, Money.Of(30m, "EUR"), Money.Of(20m, "EUR")));
    }

    [Fact]
    public void GetItem_WhenItemWasReported_ThrowsReportedItemException()
    {
        var bundle = new Bundle(ArtistId.New(), "Art Pack", LicenseType.Personal, Money.Of(15m, "EUR"));
        var itemId = BundleItemId.New();
        bundle.AddItem(itemId, "Copied Art", LicenseType.Personal);

        bundle.RemoveReportedItem(itemId);

        Assert.Throws<ReportedItemException>(() => bundle.GetItem(itemId));
    }
}