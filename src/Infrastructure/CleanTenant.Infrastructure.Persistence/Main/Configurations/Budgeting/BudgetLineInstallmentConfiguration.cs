using CleanTenant.Domain.Tenant.Budgeting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Budgeting;

/// <summary>
/// <c>budget_line_installments</c> tablosu — taksit planı satırları.
/// (BudgetLineVersionId, Year, Month) benzersiz; ay 1-12; tutar ≥ 0.
/// </summary>
internal sealed class BudgetLineInstallmentConfiguration : IEntityTypeConfiguration<BudgetLineInstallment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetLineInstallment> builder)
    {
        builder.ToTable("budget_line_installments", t =>
        {
            t.HasCheckConstraint("ck_bli_month", "month BETWEEN 1 AND 12");
            t.HasCheckConstraint("ck_bli_amount", "amount >= 0");
            t.HasCheckConstraint("ck_bli_installment_number", "installment_number > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.BudgetLineVersionId).HasColumnType("uuid").IsRequired();

        // Bir kalem versiyonunda aynı (Yıl, Ay) için tek taksit
        builder.HasIndex(x => new { x.BudgetLineVersionId, x.Year, x.Month })
            .HasDatabaseName("ix_bli_version_year_month")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.InstallmentNumber).IsRequired();
        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.Month).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(100);
        builder.Property(x => x.IsManuallyEdited).IsRequired();

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
    }
}
