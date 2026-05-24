using CleanTenant.Domain.Tenant.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Parties;

/// <summary>
/// <c>accrual_responsibility_splits</c> — bir tahakkuk detayının gün-bazlı sorumluluk
/// parçaları. Tarih CHECK + gün/tutar ≥ 0; xmin concurrency. AccrualDetail FK'si
/// AccrualDetail tarafında (Cascade) tanımlı.
/// </summary>
internal sealed class AccrualResponsibilitySplitConfiguration : IEntityTypeConfiguration<AccrualResponsibilitySplit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AccrualResponsibilitySplit> builder)
    {
        builder.ToTable("accrual_responsibility_splits", t =>
        {
            t.HasCheckConstraint("ck_ars_dates", "to_date >= from_date");
            t.HasCheckConstraint("ck_ars_daycount", "day_count > 0");
            t.HasCheckConstraint("ck_ars_amount", "amount >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.AccrualDetailId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.AccrualDetailId);

        builder.Property(x => x.PartyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.PartyId);

        builder.Property(x => x.Kind).HasConversion<short>().IsRequired();
        builder.Property(x => x.FromDate).IsRequired();
        builder.Property(x => x.ToDate).IsRequired();
        builder.Property(x => x.DayCount).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();

        builder.HasOne<Party>()
            .WithMany()
            .HasForeignKey(x => x.PartyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.UseXminAsConcurrencyToken();
    }
}
