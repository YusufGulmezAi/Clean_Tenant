using CleanTenant.Domain.Tenant.Budgeting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Budgeting;

/// <summary>
/// <c>budget_lines</c> tablosu — bütçe kalemi tanımları (versiyondan bağımsız).
/// (CompanyId, Code) benzersiz.
/// </summary>
internal sealed class BudgetLineConfiguration : IEntityTypeConfiguration<BudgetLine>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetLine> builder)
    {
        builder.ToTable("budget_lines");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.ExpenseCategoryId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.ExpenseCategoryId);

        builder.Property(x => x.AccountCodeId).HasColumnType("uuid");

        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.Code })
            .HasDatabaseName("ix_budget_lines_company_code")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();

        builder.HasOne<ExpenseCategory>()
            .WithMany()
            .HasForeignKey(x => x.ExpenseCategoryId)
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
