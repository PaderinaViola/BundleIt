using EShopService.Bundles.Domain.Aggregates;
using EShopService.Bundles.Domain.Exceptions;
using System;
using Xunit;

namespace EShopService.Bundles.Domain.Tests;

public class BundleTests
{
    [Fact]
    public void AddItem_WhenBundleIsActive_AddsItemSuccessfully()
    {
        // Arrange
        var bundle = new Bundle(Guid.NewGuid(), "Concept Art Pack", "Commercial");

        // Act
        bundle.AddItem(Guid.NewGuid(), "Environment Sketch", "Commercial");

        // Assert
        Assert.Single(bundle.Items);
    }

    [Fact]
    public void AddItem_WhenBundleIsArchived_ThrowsBundleArchivedException()
    {
        // Arrange
        var bundle = new Bundle(Guid.NewGuid(), "Season 1 Art", "Personal");
        bundle.Archive();
        var itemId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<BundleArchivedException>(() =>
            bundle.AddItem(itemId, "Late Submission", "Personal"));

        Assert.Equal("bundle.archived", ex.Code);
        Assert.Equal(bundle.Id, ex.BundleId);
    }

    [Theory]
    [InlineData("Personal", "Commercial")]
    [InlineData("Commercial", "Personal")]
    public void AddItem_WhenLicenseMismatch_ThrowsInvalidLicenseException(string bundleLicense, string itemLicense)
    {
        // Arrange
        var bundle = new Bundle(Guid.NewGuid(), "Mixed Pack", bundleLicense);

        // Act & Assert
        var ex = Assert.Throws<InvalidLicenseException>(() =>
            bundle.AddItem(Guid.NewGuid(), "Asset", itemLicense));

        Assert.Equal("bundle.invalid_license", ex.Code);
        Assert.Equal(itemLicense, ex.AttemptedLicense);
        Assert.Equal(bundleLicense, ex.RequiredLicense);
    }

    [Fact]
    public void AddCoopArtist_WhenBundleIsArchived_ThrowsBundleArchivedException()
    {
        // Arrange
        var bundle = new Bundle(Guid.NewGuid(), "Collab Pack", "Commercial");
        bundle.Archive();

        // Act & Assert
        var ex = Assert.Throws<BundleArchivedException>(() =>
            bundle.AddCoopArtist(Guid.NewGuid()));

        Assert.Equal("bundle.archived", ex.Code);
    }

    [Fact]
    public void RemoveReportedItem_WhenInvoked_MarksItemAsReported()
    {
        // Arrange
        var bundle = new Bundle(Guid.NewGuid(), "Sketchbook", "Personal");
        var itemId = Guid.NewGuid();
        bundle.AddItem(itemId, "Sketch 1", "Personal");

        // Act
        bundle.RemoveReportedItem(itemId);

        // Assert
        var ex = Assert.Throws<ReportedItemException>(() => bundle.GetItem(itemId));
        Assert.Equal(itemId, ex.ItemId);
    }
}