using CleanTenant.Domain.Tenant.Accruals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Accruals;

/// <summary>
/// <c>accruals</c> tablosu — tahakkuk başlığı. Budget kaynağı için
/// (BudgetId, AccountingPeriodId) kısmi unique (idempotency); ay 1-12 CHECK.
/// </summary>
internal sealed class AccrualConfiguration : IEntityTypeConfiguration<Accrual>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Accrual> builder)
    {
        builder.ToTable("accruals", t =>
        {
            t.HasCheckConstraint("ck_accruals_month", "month BETWEEN 1 AND 12");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.Source).HasConversion<short>().IsRequired();
        builder.Property(x => x.ResponsibilityMode).HasConversion<short>();

        builder.Property(x => x.BudgetId).HasColumnType("uuid");
        builder.Property(x => x.BudgetVersionId).HasColumnType("uuid");
        builder.Property(x => x.AccountingPeriodId).HasColumnType("uuid");
        builder.Property(x => x.InvoiceId).HasColumnType("uuid");

        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.Month).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 4).IsRequired();

        builder.Property(x => x.ReceivableAccountCodeId).HasColumnType("uuid");
        builder.Property(x => x.IncomeAccountCodeId).HasColumnType("uuid");
        builder.Property(x => x.JournalEntryId).HasColumnType("uuid");

        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.GeneratedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.GeneratedBy).HasColumnType("uuid");

        // İdempotency: Budget kaynağı (source=0) için (BudgetId, AccountingPeriodId) benzersiz.
        builder.HasIndex(x => new { x.BudgetId, x.AccountingPeriodId })
            .HasDatabaseName("ix_accruals_budget_period")
            .HasFilter("source = 0 AND is_deleted = false")
            .IsUnique();

        builder.HasMany(x => x.Details)
            .WithOne()
            .HasForeignKey(d => d.AccrualId)
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
