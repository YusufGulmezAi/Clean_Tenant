using CleanTenant.Domain.Tenant.BuildingSchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

internal sealed class ParcelConfiguration : IEntityTypeConfiguration<Parcel>
{
    public void Configure(EntityTypeBuilder<Parcel> builder)
    {
        builder.ToTable("parcels");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnType("uuid");

        builder.Property(p => p.UrlCode).HasMaxLength(9).IsRequired();
        builder.HasIndex(p => p.UrlCode).IsUnique();

        builder.Property(p => p.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(p => p.TenantId);

        builder.Property(p => p.BlockId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(p => p.BlockId);

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => new { p.BlockId, p.Name })
            .HasDatabaseName("ix_parcels_block_name")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(p => p.SortOrder).IsRequired();

        builder.HasOne(p => p.Block)
            .WithMany(b => b.Parcels)
            .HasForeignKey(p => p.BlockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.CreatedAt).HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(p => p.DeletedAt).HasColumnType("timestamptz");
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);

        builder.UseXminAsConcurrencyToken();
    }
}
