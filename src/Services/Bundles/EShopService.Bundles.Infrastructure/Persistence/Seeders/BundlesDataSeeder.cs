using EShopService.Bundles.Domain.Aggregates;
using EShopService.Bundles.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EShopService.Bundles.Infrastructure.Persistence.Seeders;

public interface IBundlesDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

internal sealed class BundlesDataSeeder : IBundlesDataSeeder
{
    public static readonly Guid DemoArtistId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CoopArtistId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid ActiveBundleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ArchivedBundleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly BundlesDbContext _dbContext;
    private readonly ILogger<BundlesDataSeeder> _logger;

    public BundlesDataSeeder(BundlesDbContext dbContext, ILogger<BundlesDataSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.IsRelational())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (await _dbContext.Bundles.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already contains bundles. Skipping BundlesDataSeeder.");
            return;
        }

        // Bundle #1: Active published commercial bundle with multiple items & co-op artist
        // Using reflection to set the private Id field for deterministic seeding
        var activeBundle = new Bundle(
            new ArtistId(DemoArtistId),
            "Inktober 2026 Pro Pack",
            LicenseType.Commercial,
            Money.Of(25.00m, "EUR"),
            Money.Of(60.00m, "EUR"),
            "Complete ink brush set, halftone textures, and layered line-art templates for commercial illustration.");

        // Override the auto-generated ID with a deterministic value for testing
        typeof(Bundle).GetProperty(nameof(Bundle.Id))?.SetValue(activeBundle, new BundleId(ActiveBundleId));

        activeBundle.AddCoopArtist(new ArtistId(CoopArtistId));
        activeBundle.AddItem(
            BundleItemId.New(),
            "Custom Ink Brushset",
            LicenseType.Commercial,
            "45 pressure-sensitive dip pen and nib brushes for Procreate and Photoshop.",
            "ABR",
            48_500_000);
        activeBundle.AddItem(
            BundleItemId.New(),
            "Screentone & Paper Textures",
            LicenseType.Commercial,
            "300 DPI cold-press paper scans and manga halftone overlays.",
            "ZIP",
            124_000_000);
        activeBundle.Publish();

        // Bundle #2: Archived bundle (edge-case for testing BundleArchivedException -> HTTP 422)
        var archivedBundle = new Bundle(
            new ArtistId(DemoArtistId),
            "Legacy Sketchbook 2024",
            LicenseType.Personal,
            Money.Of(15.00m, "EUR"),
            description: "Retired 2024 observational figure and charcoal study archive for personal study.");

        typeof(Bundle).GetProperty(nameof(Bundle.Id))?.SetValue(archivedBundle, new BundleId(ArchivedBundleId));

        archivedBundle.AddItem(
            BundleItemId.New(),
            "Charcoal Studies",
            LicenseType.Personal,
            "High-resolution scans of 30 traditional charcoal gesture drawings.",
            "PDF",
            32_400_000);
        archivedBundle.Publish();
        archivedBundle.Archive();

        await _dbContext.Bundles.AddRangeAsync([activeBundle, archivedBundle], cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded 2 initial bundles. ActiveBundleId: {ActiveBundleId}, ArchivedBundleId: {ArchivedBundleId}, DemoArtistId: {DemoArtistId}",
            activeBundle.Id.Value,
            archivedBundle.Id.Value,
            DemoArtistId);
    }
}

public sealed record CreateBundleRequest(
    Guid PrimaryArtistId,
    string Title,
    string PackLicenseType,
    decimal PersonalPrice,
    string Currency,
    decimal? CommercialPrice = null,
    string Description = "Digital asset bundle");

public sealed record AddBundleItemRequest(
    Guid ItemId,
    string Name,
    string LicenseType,
    string Description = "Digital bundle asset",
    string FileFormat = "ZIP",
    long FileSizeBytes = 1024);