using EShopService.Bundles.Application.Repositories;
using EShopService.Bundles.Application.Services;
using EShopService.Bundles.Domain.Aggregates;
using EShopService.Bundles.Domain.Exceptions;
using EShopService.Bundles.Domain.ValueObjects;
using Moq;
using Xunit;

namespace EShopService.Bundles.Application.Tests;

public sealed class BundleServiceTests
{
    [Fact]
    public async Task CreateBundleAsync_ValidInput_SavesAndReturnsDto()
    {
        var repoMock = new Mock<IBundleRepository>();
        var service = new BundleService(repoMock.Object);
        var artistId = Guid.NewGuid();

        var result = await service.CreateBundleAsync(artistId, "Fantasy Brushes", "Commercial", 25m, "EUR", 50m);

        Assert.Equal("Fantasy Brushes", result.Title);
        Assert.Equal(25m, result.PersonalPrice);
        Assert.Equal(50m, result.CommercialPrice);
        repoMock.Verify(r => r.SaveAsync(It.IsAny<Bundle>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_ArchivedBundle_ThrowsAndNeverSaves()
    {
        var bundle = new Bundle(ArtistId.New(), "Old Pack", LicenseType.Personal, Money.Of(10m, "EUR"));
        bundle.Archive();

        var repoMock = new Mock<IBundleRepository>();
        repoMock.Setup(r => r.GetByIdAsync(bundle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bundle);
        var service = new BundleService(repoMock.Object);

        await Assert.ThrowsAsync<BundleArchivedException>(() =>
            service.AddItemAsync(bundle.Id.Value, Guid.NewGuid(), "New File", "Personal"));

        repoMock.Verify(r => r.SaveAsync(It.IsAny<Bundle>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}