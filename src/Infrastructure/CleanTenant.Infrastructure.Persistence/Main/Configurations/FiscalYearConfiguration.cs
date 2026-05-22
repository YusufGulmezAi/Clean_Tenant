using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>fiscal_years</c> tablosu için EF Core map'i.
/// Tarih tutarlılığı CHECK, süre aralığı CHECK (330–400 gün),
/// muhasebe dönemleri cascade kısıtı ve xmin concurrency token.
/// </summary>
internal sealed class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.ToTable("fiscal_years", t =>
        {
            t.HasCheckConstraint("ck_fiscal_year_dates",
                "end_date > start_date");
            // Mali yılın uzunluğu 330–400 gün arasında (takvim dışı dönemler için tolerans)
            t.HasCheckConstraint("ck_fiscal_year_duration",
                "end_date - start_date BETWEEN 330 AND 400");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        // CompanyId + StartDate kombinasyonu benzersiz (soft-delete hariç)
        builder.HasIndex(x => new { x.CompanyId, x.StartDate })
            .HasDatabaseName("ix_fiscal_years_company_start")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.Label).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<short>().IsRequired();
        builder.Property(x => x.IsCurrentYear).IsRequired();

        // Muhasebe dönemleri ilişkisi — Restrict: dönem varken mali yıl silinemez
        builder.HasMany(fy => fy.Periods)
            .WithOne(p => p.FiscalYear)
            .HasForeignKey(p => p.FiscalYearId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit + soft delete (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin concurrency token
        builder.UseXminAsConcurrencyToken();
    }
}
