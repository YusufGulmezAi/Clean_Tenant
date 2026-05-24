using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.Domain.Tenant.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Accruals;

/// <summary>
/// <c>accrual_details</c> tablosu — BB-bazlı tahakkuk yardımcı defteri.
/// (AccrualId, UnitId) benzersiz; tutar ≥ 0.
/// </summary>
internal sealed class AccrualDetailConfiguration : IEntityTypeConfiguration<AccrualDetail>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AccrualDetail> builder)
    {
        builder.ToTable("accrual_details", t =>
        {
            t.HasCheckConstraint("ck_accrual_details_amount", "amount >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.AccrualId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.UnitId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.UnitId);

        // Bir tahakkukta bir BB tek satır
        builder.HasIndex(x => new { x.AccrualId, x.UnitId })
            .HasDatabaseName("ix_accrual_details_accrual_unit")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.DistributionShare).HasPrecision(18, 8).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.LineBreakdownJson).HasColumnType("jsonb");

        // Sorumluluk (F0) — birincil sorumlu + çözümleme notu + gün-bazlı parçalar
        builder.Property(x => x.PrimaryResponsiblePartyId).HasColumnType("uuid");
        builder.HasIndex(x => x.PrimaryResponsiblePartyId);
        builder.Property(x => x.ResponsibleResolvedNote).HasMaxLength(200);
        builder.HasOne<Party>()
            .WithMany()
            .HasForeignKey(x => x.PrimaryResponsiblePartyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Responsibilities)
            .WithOne()
            .HasForeignKey(r => r.AccrualDetailId)
            .OnDelete(DeleteBehavior.Cascade);

        // BB FK — restrict
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
