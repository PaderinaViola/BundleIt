using EShopService.Bundles.Domain.Aggregates;
using EShopService.Bundles.Domain.Aggregates;
using EShopService.Bundles.Domain.Entities;
using EShopService.Bundles.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopService.Bundles.Infrastructure.Persistence.Configurations;

internal sealed class BundleConfiguration : IEntityTypeConfiguration<Bundle>
{
    public void Configure(EntityTypeBuilder<Bundle> builder)
    {
        builder.ToTable("Bundles", schema: "bundles");

        // 1. Strongly typed identifiers (ValueGeneratedNever because Guid is created in C#)
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(id => id.Value, value => new BundleId(value))
            .ValueGeneratedNever();

        builder.Property(b => b.PrimaryArtistId)
            .HasConversion(id => id.Value, value => new ArtistId(value))
            .IsRequired();

        builder.HasIndex(b => b.PrimaryArtistId);

        builder.Property(b => b.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(b => b.CreatedAtUtc)
            .IsRequired();

        builder.Property(b => b.PublishedAtUtc);

        // 2. Enums stored as readable strings
        builder.Property(b => b.PackLicenseType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        // 3. Multi-field Money Value Objects flattened into columns of bundles.Bundles
        builder.OwnsOne(b => b.PersonalPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("PersonalPriceAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("PersonalPriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(b => b.CommercialPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("CommercialPriceAmount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("CommercialPriceCurrency")
                .HasMaxLength(3);
        });

        // 4. Internal BundleItem entities mapped to bundles.BundleItems via backing field
        builder.OwnsMany(b => b.Items, itemBuilder =>
        {
            itemBuilder.ToTable("BundleItems", schema: "bundles");
            itemBuilder.WithOwner().HasForeignKey("BundleId");
            itemBuilder.HasKey("BundleId", nameof(BundleItem.ItemId));

            itemBuilder.Property(i => i.ItemId)
                .HasConversion(id => id.Value, value => new BundleItemId(value))
                .ValueGeneratedNever();

            itemBuilder.Property(i => i.Name)
                .HasMaxLength(200)
                .IsRequired();

            itemBuilder.Property(i => i.Description)
                .HasMaxLength(500)
                .IsRequired();

            itemBuilder.Property(i => i.License)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            itemBuilder.Property(i => i.FileFormat)
                .HasMaxLength(20)
                .IsRequired();

            itemBuilder.Property(i => i.FileSizeBytes)
                .IsRequired();

            itemBuilder.Property(i => i.IsReported)
                .IsRequired();

            itemBuilder.Property(i => i.AddedAtUtc)
                .IsRequired();
        });

        builder.Navigation(b => b.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // 5. Co-op artist IDs backing field
        builder.PrimitiveCollection<List<Guid>>("_coopArtistIds")
            .HasColumnName("CoopArtistIds");

        builder.Ignore(b => b.IsArchived);
        builder.Ignore(b => b.CoopArtists);
    }
}