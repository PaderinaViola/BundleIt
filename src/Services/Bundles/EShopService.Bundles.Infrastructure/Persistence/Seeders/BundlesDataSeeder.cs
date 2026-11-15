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

        // Bundle #1: Active published commercial bundle
        var activeBundle = new Bundle(
            new ArtistId(DemoArtistId),
            "Inktober 2026 Pro Pack",
            LicenseType.Commercial,
            Money.Of(25.00m, "EUR"),
            Money.Of(60.00m, "EUR"));
        activeBundle.AddItem(BundleItemId.New(), "Custom Ink Brushset", LicenseType.Commercial);
        activeBundle.Publish();

        // Bundle #2: Archived bundle (edge-case for testing BundleArchivedException -> HTTP 422)
        var archivedBundle = new Bundle(
            new ArtistId(DemoArtistId),
            "Legacy Sketchbook 2024",
            LicenseType.Personal,
            Money.Of(15.00m, "EUR"));
        archivedBundle.AddItem(BundleItemId.New(), "Charcoal Studies", LicenseType.Personal);
        archivedBundle.Publish();
        archivedBundle.Archive();

        await _dbContext.Bundles.AddRangeAsync([activeBundle, archivedBundle], cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded 2 initial bundles. ActiveBundleId: {ActiveBundleId}, ArchivedBundleId: {ArchivedBundleId}",
            activeBundle.Id.Value,
            archivedBundle.Id.Value);
    }
}