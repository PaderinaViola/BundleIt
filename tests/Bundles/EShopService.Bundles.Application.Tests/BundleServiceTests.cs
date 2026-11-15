using EShopService.Bundles.Application.Repositories;
using EShopService.Bundles.Application.Services;
using EShopService.Bundles.Domain.Aggregates;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EShopService.Bundles.Application.Tests;

public class BundleServiceTests
{
    private readonly Mock<IBundleRepository> _repositoryMock = new();
    private readonly BundleService _sut;

    public BundleServiceTests()
    {
        _sut = new BundleService(_repositoryMock.Object);
    }

    [Fact]
    public async Task PublishBundleAsync_WhenValidData_CreatesAndSavesBundle()
    {
        // Arrange
        var artistId = Guid.NewGuid();
        var title = "Ultimate 3D Pack";
        var license = "Commercial";

        // Act
        var result = await _sut.PublishBundleAsync(artistId, title, license);

        // Assert
        Assert.NotEqual(Guid.Empty, result.BundleId);
        Assert.Equal(artistId, result.PrimaryArtistId);
        Assert.Equal(title, result.Title);
        _repositoryMock.Verify(
            r => r.SaveAsync(It.Is<Bundle>(b => b.Title == title), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_WhenBundleExists_UpdatesAndSavesBundle()
    {
        // Arrange
        var existingBundle = new Bundle(Guid.NewGuid(), "Textures Pack", "Personal");
        _repositoryMock
            .Setup(r => r.GetByIdAsync(existingBundle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBundle);

        // Act
        var result = await _sut.AddItemAsync(existingBundle.Id, Guid.NewGuid(), "Brick Texture", "Personal");

        // Assert
        Assert.Single(result.Items);
        _repositoryMock.Verify(
            r => r.SaveAsync(existingBundle, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}