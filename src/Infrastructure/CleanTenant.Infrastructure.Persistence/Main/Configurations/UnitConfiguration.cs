using CleanTenant.Domain.Tenant.BuildingSchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("units");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnType("uuid");

        builder.Property(u => u.UrlCode).HasMaxLength(9).IsRequired();
        builder.HasIndex(u => u.UrlCode).IsUnique();

        builder.Property(u => u.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(u => u.TenantId);

        builder.Property(u => u.BuildingId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(u => u.BuildingId);

        builder.Property(u => u.Number).HasMaxLength(20).IsRequired();
        builder.HasIndex(u => new { u.BuildingId, u.Number })
            .HasDatabaseName("ix_units_building_number")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(u => u.NationalAddressCode).HasMaxLength(50);
        builder.Property(u => u.Type).HasConversion<short>().IsRequired();
        builder.Property(u => u.SquareMeters).HasColumnType("decimal(10,2)").IsRequired();
        builder.Property(u => u.LandShare).IsRequired();
        builder.Property(u => u.AllocatedArea).HasColumnType("decimal(10,2)");
        builder.Property(u => u.Floor).IsRequired();
        builder.Property(u => u.Orientation).HasConversion<short>().IsRequired();
        builder.Property(u => u.Layout).HasConversion<short>().IsRequired();
        builder.Property(u => u.SortOrder).IsRequired();

        builder.HasOne(u => u.Building)
            .WithMany(b => b.Units)
            .HasForeignKey(u => u.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(u => u.CreatedAt).HasColumnType("timestamptz");
        builder.Property(u => u.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(u => u.DeletedAt).HasColumnType("timestamptz");
        builder.Property(u => u.IsDeleted).HasDefaultValue(false);

        builder.UseXminAsConcurrencyToken();
    }
}
