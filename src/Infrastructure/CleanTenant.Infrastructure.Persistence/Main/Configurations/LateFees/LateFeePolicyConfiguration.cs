using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.LateFees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.LateFees;

/// <summary>
/// <c>late_fee_policies</c> tablosu — gecikme faizi politikası. Şirket-geneli
/// varsayılan (budget_id NULL) ve bütçe override (budget_id dolu) için iki ayrı
/// kısmi unique index. monthly_rate_percent > 0, grace_days ≥ 0 CHECK.
/// </summary>
internal sealed class LateFeePolicyConfiguration : IEntityTypeConfiguration<LateFeePolicy>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LateFeePolicy> builder)
    {
        builder.ToTable("late_fee_policies", t =>
        {
            t.HasCheckConstraint("ck_late_fee_policies_rate", "monthly_rate_percent > 0");
            t.HasCheckConstraint("ck_late_fee_policies_grace", "grace_days >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.BudgetId).HasColumnType("uuid");

        builder.Property(x => x.MonthlyRatePercent).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.IsCompound).HasDefaultValue(false);
        builder.Property(x => x.GraceDays).IsRequired();
        builder.Property(x => x.IncomeAccountCodeId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Şirket-geneli varsayılan: company başına tek (budget_id NULL)
        builder.HasIndex(x => x.CompanyId)
            .HasDatabaseName("ix_late_fee_policies_company_default")
            .HasFilter("budget_id IS NULL AND is_deleted = false")
            .IsUnique();

        // Bütçe override: bütçe başına tek (budget_id dolu)
        builder.HasIndex(x => x.BudgetId)
            .HasDatabaseName("ix_late_fee_policies_budget_override")
            .HasFilter("budget_id IS NOT NULL AND is_deleted = false")
            .IsUnique();

        // FK'ler — restrict (politika varken bütçe/hesap silinmesin)
        builder.HasOne<Budget>()
            .WithMany()
            .HasForeignKey(x => x.BudgetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AccountCode>()
            .WithMany()
            .HasForeignKey(x => x.IncomeAccountCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin concurrency token
        builder.UseXminAsConcurrencyToken();
    }
}
