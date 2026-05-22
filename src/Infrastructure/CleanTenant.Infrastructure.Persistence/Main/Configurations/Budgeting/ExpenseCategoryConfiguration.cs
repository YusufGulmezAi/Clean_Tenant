using CleanTenant.Domain.Tenant.Budgeting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Budgeting;

/// <summary>
/// <c>expense_categories</c> tablosu — gider kategorisi hiyerarşisi.
/// (CompanyId, Code) çifti benzersiz; self-referencing parent FK Restrict.
/// </summary>
internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("expense_categories");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.ParentCategoryId).HasColumnType("uuid");
        builder.HasIndex(x => x.ParentCategoryId);

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();

        // (CompanyId, Code) çifti benzersiz
        builder.HasIndex(x => new { x.CompanyId, x.Code })
            .HasDatabaseName("ix_expense_categories_company_code")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.DisplayOrder).IsRequired();

        // Self-referencing parent FK
        builder.HasOne<ExpenseCategory>()
            .WithMany()
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
    }
}
