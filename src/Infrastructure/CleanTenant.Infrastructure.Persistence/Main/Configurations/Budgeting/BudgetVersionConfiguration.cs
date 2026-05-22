using CleanTenant.Domain.Tenant.Budgeting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Budgeting;

/// <summary>
/// <c>budget_versions</c> tablosu — yayınlanmış bütçe versiyonu (V1, V2, …).
/// (BudgetId, VersionNumber) benzersiz; ValidFrom ≤ ValidTo CHECK.
/// </summary>
internal sealed class BudgetVersionConfiguration : IEntityTypeConfiguration<BudgetVersion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetVersion> builder)
    {
        builder.ToTable("budget_versions", t =>
        {
            t.HasCheckConstraint("ck_budget_version_dates",
                "valid_to IS NULL OR valid_to >= valid_from");
            t.HasCheckConstraint("ck_budget_version_number",
                "version_number > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.BudgetId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.BudgetId);

        builder.Property(x => x.VersionNumber).IsRequired();

        // (BudgetId, VersionNumber) çifti benzersiz
        builder.HasIndex(x => new { x.BudgetId, x.VersionNumber })
            .HasDatabaseName("ix_budget_versions_budget_version")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ValidTo);

        builder.Property(x => x.PreviousVersionId).HasColumnType("uuid");
        builder.Property(x => x.PublishedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.PublishedBy).HasColumnType("uuid");
        builder.Property(x => x.RevisionReason).HasMaxLength(1000);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // Child entity — xmin yok
    }
}
