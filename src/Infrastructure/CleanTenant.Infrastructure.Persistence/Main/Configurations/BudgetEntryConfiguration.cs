using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>budgets</c> tablosu için EF Core map'i (legacy <see cref="BudgetEntry"/>).
/// CompanyId + AccountingPeriodId + AccountCodeId + CostCenterId kombinasyonu
/// benzersiz; negatif bütçe yasağı CHECK.
/// IAggregateRoot değil — xmin token eklenmez.
/// v0.2.13.a: Class Budget → BudgetEntry yeniden adlandırıldı (DB tablosu aynı).
/// </summary>
internal sealed class BudgetEntryConfiguration : IEntityTypeConfiguration<BudgetEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetEntry> builder)
    {
        builder.ToTable("budgets", t =>
        {
            t.HasCheckConstraint("ck_budget_amount",
                "budgeted_amount >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.AccountingPeriodId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.AccountingPeriodId);

        builder.Property(x => x.AccountCodeId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.AccountCodeId);

        builder.Property(x => x.CostCenterId).HasColumnType("uuid");

        // CompanyId + Period + AccountCode + CostCenter kombinasyonu benzersiz (soft-delete hariç)
        builder.HasIndex(x => new { x.CompanyId, x.AccountingPeriodId, x.AccountCodeId, x.CostCenterId })
            .HasDatabaseName("ix_budgets_company_period_account_costcenter")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.BudgetedAmount).HasPrecision(18, 4).IsRequired();

        // Audit + soft delete (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // NOT UseXminAsConcurrencyToken — IAggregateRoot değil
    }
}
