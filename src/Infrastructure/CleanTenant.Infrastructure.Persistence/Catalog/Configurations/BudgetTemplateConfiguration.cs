using CleanTenant.Domain.Budgeting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <c>budget_templates</c> tablosu — tenant'lar-arası paylaşılabilir bütçe şablonu
/// (Catalog, app-global). UrlCode unique; tenant izolasyonu yok, xmin yok.
/// </summary>
internal sealed class BudgetTemplateConfiguration : IEntityTypeConfiguration<BudgetTemplate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetTemplate> builder)
    {
        builder.ToTable("budget_templates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.OwnerTenantId).HasColumnType("uuid");
        builder.HasIndex(x => x.OwnerTenantId);

        builder.Property(x => x.Visibility).HasConversion<short>().IsRequired();
        builder.Property(x => x.Type).HasConversion<short>().IsRequired();
        builder.HasIndex(x => x.Type);

        builder.Property(x => x.UrlCode).HasMaxLength(9).IsRequired();
        builder.HasIndex(x => x.UrlCode).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.SourceLabel).HasMaxLength(256);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.BudgetTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // NO xmin — Catalog katalog, tenant-scoped değil
    }
}
