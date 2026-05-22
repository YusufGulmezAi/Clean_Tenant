using CleanTenant.Domain.Budgeting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <c>budget_type_metadata</c> tablosu için EF Core map'i.
/// Catalog DB'de tüm tenant'ların paylaştığı bütçe tipi kataloğu (base hesap
/// kodları). Type benzersiz; tenant izolasyonu yoktur, xmin token eklenmez.
/// </summary>
internal sealed class BudgetTypeMetadataConfiguration : IEntityTypeConfiguration<BudgetTypeMetadata>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetTypeMetadata> builder)
    {
        builder.ToTable("budget_type_metadata");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.Type).HasConversion<short>().IsRequired();
        builder.HasIndex(x => x.Type)
            .HasDatabaseName("ix_budget_type_metadata_type")
            .IsUnique();

        builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BaseReceivableCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.BaseIncomeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DefaultPaymentSchedule).HasConversion<short>().IsRequired();
        builder.Property(x => x.AllowMultiplePerYear).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        // Audit (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // NO xmin — Catalog katalog, tenant-scoped değil
    }
}
