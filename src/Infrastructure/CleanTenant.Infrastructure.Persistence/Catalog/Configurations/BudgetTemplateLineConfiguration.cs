using CleanTenant.Domain.Budgeting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <c>budget_template_lines</c> tablosu — şablon kalemleri (yapı-only, denormalize).
/// (BudgetTemplateId, LineCode) benzersiz.
/// </summary>
internal sealed class BudgetTemplateLineConfiguration : IEntityTypeConfiguration<BudgetTemplateLine>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetTemplateLine> builder)
    {
        builder.ToTable("budget_template_lines");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.BudgetTemplateId).HasColumnType("uuid").IsRequired();

        builder.Property(x => x.CategoryCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CategoryName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ParentCategoryCode).HasMaxLength(20);
        builder.Property(x => x.LineCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.LineName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LineDescription).HasMaxLength(1000);

        builder.Property(x => x.PaymentSchedule).HasConversion<short>().IsRequired();
        builder.Property(x => x.DistributionModel).HasConversion<short>().IsRequired();
        builder.Property(x => x.DistributionConfig);
        builder.Property(x => x.DueDayOfMonth).IsRequired();

        builder.Property(x => x.ParticipationGroupCode).HasMaxLength(20);
        builder.Property(x => x.ParticipationGroupName).HasMaxLength(200);
        builder.Property(x => x.InstallmentIntervalMonths);
        builder.Property(x => x.InstallmentCount);
        builder.Property(x => x.DisplayOrder).IsRequired();

        builder.HasIndex(x => new { x.BudgetTemplateId, x.LineCode })
            .HasDatabaseName("ix_budget_template_lines_template_line")
            .HasFilter("is_deleted = false")
            .IsUnique();

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // NO xmin — Catalog katalog
    }
}
