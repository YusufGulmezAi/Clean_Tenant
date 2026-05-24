using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.Domain.Tenant.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Parties;

/// <summary>
/// <c>unit_contacts</c> — İletişim kişisi tenure. Tarih CHECK; xmin concurrency.
/// </summary>
internal sealed class UnitContactConfiguration : IEntityTypeConfiguration<UnitContact>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UnitContact> builder)
    {
        builder.ToTable("unit_contacts", t =>
        {
            t.HasCheckConstraint("ck_unit_contacts_dates", "end_date IS NULL OR end_date >= start_date");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.UnitId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.UnitId);

        builder.Property(x => x.PartyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.PartyId);

        builder.Property(x => x.ContactRole).HasConversion<short>().IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate);
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
