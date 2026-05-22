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
        builder.ToTable("budget_plans");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.FiscalYearId).HasColumnType("uuid").IsRequired();

        // (CompanyId, FiscalYearId) çifti benzersiz — 1 Site × 1 Mali Yıl = 1 Bütçe (Karar #2).
        builder.HasIndex(x => new { x.CompanyId, x.FiscalYearId })
            .HasDatabaseName("ix_budget_plans_company_fy")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.UrlCode).HasMaxLength(9).IsRequired();
        builder.HasIndex(x => x.UrlCode).IsUnique();

        builder.Property(x => x.Title).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.Status).HasConversion<short>().IsRequired();

        builder.Property(x => x.CurrentVersionId).HasColumnType("uuid");

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
