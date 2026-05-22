using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>accounting_periods</c> tablosu için EF Core map'i.
/// Ay aralığı CHECK, tarih tutarlılığı CHECK ve CompanyId+Year+Month benzersizliği.
/// ITenantScoped ama IAggregateRoot değil — xmin token eklenmez.
/// </summary>
internal sealed class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.ToTable("accounting_periods", t =>
        {
            t.HasCheckConstraint("ck_accounting_period_month",
                "month BETWEEN 1 AND 12");
            t.HasCheckConstraint("ck_accounting_period_dates",
                "end_date > start_date");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.FiscalYearId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.FiscalYearId);

        // CompanyId + Year + Month kombinasyonu benzersiz (soft-delete hariç)
        builder.HasIndex(x => new { x.CompanyId, x.Year, x.Month })
            .HasDatabaseName("ix_accounting_periods_company_year_month")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.Month).IsRequired();
        builder.Property(x => x.Status).HasConversion<short>().IsRequired();

        // Audit + soft delete (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // NOT UseXminAsConcurrencyToken — IAggregateRoot değil
    }
}
