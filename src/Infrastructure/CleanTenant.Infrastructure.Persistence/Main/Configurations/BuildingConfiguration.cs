using CleanTenant.Domain.Tenant.BuildingSchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

internal sealed class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("buildings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnType("uuid");

        builder.Property(b => b.UrlCode).HasMaxLength(9).IsRequired();
        builder.HasIndex(b => b.UrlCode).IsUnique();

        builder.Property(b => b.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(b => b.TenantId);

        builder.Property(b => b.ParcelId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(b => b.ParcelId);

        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(b => new { b.ParcelId, b.Name })
            .HasDatabaseName("ix_buildings_parcel_name")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(b => b.MunicipalNo).HasMaxLength(50);
        builder.Property(b => b.Type).HasConversion<short>().IsRequired();
        builder.Property(b => b.SortOrder).IsRequired();

        builder.HasOne(b => b.Parcel)
            .WithMany(p => p.Buildings)
            .HasForeignKey(b => b.ParcelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.CreatedAt).HasColumnType("timestamptz");
        builder.Property(b => b.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(b => b.DeletedAt).HasColumnType("timestamptz");
        builder.Property(b => b.IsDeleted).HasDefaultValue(false);

        builder.UseXminAsConcurrencyToken();
    }
}
