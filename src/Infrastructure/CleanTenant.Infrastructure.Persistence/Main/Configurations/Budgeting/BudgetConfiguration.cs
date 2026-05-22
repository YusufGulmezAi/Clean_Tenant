using CleanTenant.Domain.Tenant.Budgeting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Budgeting;

/// <summary>
/// <c>budget_plans</c> tablosu için EF Core map'i — yeni <see cref="Budget"/>
/// aggregate'i (FAZ 5). Eski <c>budgets</c> tablosundan ayrı; geçici legacy
/// <c>BudgetEntry</c> ile birlikte yaşar, Slice 4d'de eski kaldırılınca tablo
/// adı <c>budgets</c>'a yeniden adlandırılabilir.
/// </summary>
internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budget_plans", t =>
        {
            t.HasCheckConstraint("ck_budget_plans_period_months",
                "period_start_month BETWEEN 1 AND 12 AND period_end_month BETWEEN 1 AND 12");
            // Bitiş >= Başlangıç (yıl*12+ay karşılaştırması)
            t.HasCheckConstraint("ck_budget_plans_period_order",
                "(period_end_year * 12 + period_end_month) >= (period_start_year * 12 + period_start_month)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.FiscalYearId).HasColumnType("uuid").IsRequired();

        builder.Property(x => x.Type).HasConversion<short>().IsRequired();

        // v0.2.14 — (CompanyId, FiscalYearId, Type, Title) çifti benzersiz.
        // Aynı yılda aynı tipte birden fazla bütçe olabilir (ek aidat, çoklu yatırım),
        // ama aynı isimle iki tane olamaz.
        builder.HasIndex(x => new { x.CompanyId, x.FiscalYearId, x.Type, x.Title })
            .HasDatabaseName("ix_budget_plans_company_fy_type_title")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.UrlCode).HasMaxLength(9).IsRequired();
        builder.HasIndex(x => x.UrlCode).IsUnique();

        builder.Property(x => x.Title).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);

        // Bütçe geçerlilik dönemi
        builder.Property(x => x.PeriodStartYear).IsRequired();
        builder.Property(x => x.PeriodStartMonth).IsRequired();
        builder.Property(x => x.PeriodEndYear).IsRequired();
        builder.Property(x => x.PeriodEndMonth).IsRequired();

        builder.Property(x => x.Status).HasConversion<short>().IsRequired();

        builder.Property(x => x.CurrentVersionId).HasColumnType("uuid");

        // İlk tahakkukta otomatik üretilen alt hesap kodları (nullable)
        builder.Property(x => x.ReceivableAccountCodeId).HasColumnType("uuid");
        builder.Property(x => x.IncomeAccountCodeId).HasColumnType("uuid");

        // Versions navigation (BudgetVersion.BudgetId üzerinden)
        builder.HasMany(x => x.Versions)
            .WithOne()
            .HasForeignKey(v => v.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin concurrency token
        builder.UseXminAsConcurrencyToken();
    }
}
