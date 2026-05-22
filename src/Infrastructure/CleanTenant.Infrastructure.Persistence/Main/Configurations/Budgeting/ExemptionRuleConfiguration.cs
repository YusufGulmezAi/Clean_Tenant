using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.BuildingSchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Budgeting;

/// <summary>
/// <c>exemption_rules</c> — muafiyet kuralları (BB belirli kalemden tarih aralığında muaf).
/// ValidFrom ≤ ValidTo CHECK.
/// </summary>
internal sealed class ExemptionRuleConfiguration : IEntityTypeConfiguration<ExemptionRule>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExemptionRule> builder)
    {
        builder.ToTable("exemption_rules", t =>
        {
            t.HasCheckConstraint("ck_exemption_dates", "valid_to IS NULL OR valid_to >= valid_from");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.UnitId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.UnitId);

        builder.Property(x => x.BudgetLineId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.BudgetLineId);

        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ValidTo);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();

        // FK'ler
        builder.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BudgetLine>()
            .WithMany()
            .HasForeignKey(x => x.BudgetLineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
    }
}
