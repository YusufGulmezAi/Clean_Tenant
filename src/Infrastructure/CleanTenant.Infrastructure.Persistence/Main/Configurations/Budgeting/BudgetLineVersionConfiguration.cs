using CleanTenant.Domain.Tenant.Budgeting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Budgeting;

/// <summary>
/// <c>budget_line_versions</c> tablosu — bir versiyonda bir kalem için planlanan
/// tutar + dağıtım. (BudgetVersionId, BudgetLineId) benzersiz; planned amount ≥ 0;
/// due day 1-31.
/// </summary>
internal sealed class BudgetLineVersionConfiguration : IEntityTypeConfiguration<BudgetLineVersion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetLineVersion> builder)
    {
        builder.ToTable("budget_line_versions", t =>
        {
            t.HasCheckConstraint("ck_blv_planned_amount", "planned_amount >= 0");
            t.HasCheckConstraint("ck_blv_due_day", "due_day_of_month BETWEEN 1 AND 31");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.BudgetVersionId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.BudgetLineId).HasColumnType("uuid").IsRequired();

        // Bir versiyonda aynı kalem iki kez bulunamaz
        builder.HasIndex(x => new { x.BudgetVersionId, x.BudgetLineId })
            .HasDatabaseName("ix_blv_version_line")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.PlannedAmount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.PaymentSchedule).HasConversion<short>().IsRequired();
        builder.Property(x => x.DistributionModel).HasConversion<short>().IsRequired();
        builder.Property(x => x.ParticipationGroupId).HasColumnType("uuid");
        builder.Property(x => x.DistributionConfig).HasColumnType("jsonb");
        builder.Property(x => x.IsManualOverride).IsRequired();
        builder.Property(x => x.OverrideReason).HasMaxLength(500);
        builder.Property(x => x.DueDayOfMonth).IsRequired();

        // Installment konfig (PaymentSchedule == Installment için; aksi halde null)
        builder.Property(x => x.InstallmentStartYear);
        builder.Property(x => x.InstallmentStartMonth);
        builder.Property(x => x.InstallmentEndYear);
        builder.Property(x => x.InstallmentEndMonth);
        builder.Property(x => x.InstallmentIntervalMonths);

        // FK'ler — silinmeyi engelle
        builder.HasOne<BudgetVersion>()
            .WithMany()
            .HasForeignKey(x => x.BudgetVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<BudgetLine>()
            .WithMany()
            .HasForeignKey(x => x.BudgetLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ParticipationGroup>()
            .WithMany()
            .HasForeignKey(x => x.ParticipationGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Installment satırları (Installment plan)
        builder.HasMany(x => x.Installments)
            .WithOne()
            .HasForeignKey(i => i.BudgetLineVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
    }
}
