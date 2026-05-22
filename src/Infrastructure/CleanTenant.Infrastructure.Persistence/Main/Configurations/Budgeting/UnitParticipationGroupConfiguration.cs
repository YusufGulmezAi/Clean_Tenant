using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.BuildingSchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Budgeting;

/// <summary>
/// <c>unit_participation_groups</c> — ParticipationGroup ↔ Unit junction.
/// (ParticipationGroupId, UnitId) benzersiz; ValidFrom ≤ ValidTo CHECK.
/// </summary>
internal sealed class UnitParticipationGroupConfiguration : IEntityTypeConfiguration<UnitParticipationGroup>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UnitParticipationGroup> builder)
    {
        builder.ToTable("unit_participation_groups", t =>
        {
            t.HasCheckConstraint("ck_upg_dates", "valid_to IS NULL OR valid_to >= valid_from");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.ParticipationGroupId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.UnitId).HasColumnType("uuid").IsRequired();

        // (ParticipationGroupId, UnitId) çifti benzersiz
        builder.HasIndex(x => new { x.ParticipationGroupId, x.UnitId })
            .HasDatabaseName("ix_upg_group_unit")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ValidTo);
        builder.Property(x => x.Notes).HasMaxLength(500);

        // FK'ler
        builder.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
    }
}
