using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.Domain.Tenant.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Parties;

/// <summary>
/// <c>unit_ownerships</c> — Malik tenure (pay% + müteselsil). Tarih ve pay CHECK;
/// xmin concurrency.
/// </summary>
internal sealed class UnitOwnershipConfiguration : IEntityTypeConfiguration<UnitOwnership>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UnitOwnership> builder)
    {
        builder.ToTable("unit_ownerships", t =>
        {
            t.HasCheckConstraint("ck_unit_ownerships_dates", "end_date IS NULL OR end_date >= start_date");
            t.HasCheckConstraint("ck_unit_ownerships_share", "share_percent > 0 AND share_percent <= 100");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.UnitId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.UnitId);

        builder.Property(x => x.PartyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.PartyId);

        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate);
        builder.Property(x => x.SharePercent).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.IsJointAndSeveral).HasDefaultValue(false);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.UseXminAsConcurrencyToken();
    }
}
