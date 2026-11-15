using EShopService.Bundles.Application.Repositories;
using EShopService.Bundles.Infrastructure.Persistence;
using EShopService.Bundles.Infrastructure.Persistence.Repositories;
using EShopService.Bundles.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EShopService.Bundles.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        bool.TryParse(configuration["Persistence:UseInMemoryDatabase"], out var useInMemory);

        services.AddDbContext<BundlesDbContext>(options =>
        {
            if (useInMemory)
            {
                var dbName = configuration["Persistence:InMemoryDatabaseName"] ?? "EShopBundlesInMemoryDb";
                options.UseInMemoryDatabase(dbName);
            }
            else
            {
                var connectionString = configuration.GetConnectionString("BundlesDb")
                    ?? throw new InvalidOperationException("Missing connection string 'BundlesDb' in configuration.");

                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bundles");
                });
            }
        });

        services.AddScoped<IBundleRepository, SqlBundleRepository>();
        services.AddScoped<IBundlesDataSeeder, BundlesDataSeeder>();

        return services;
    }
}