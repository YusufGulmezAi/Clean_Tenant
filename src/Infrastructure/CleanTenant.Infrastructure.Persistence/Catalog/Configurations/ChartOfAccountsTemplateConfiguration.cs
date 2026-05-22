using CleanTenant.Domain.LookUp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <c>chart_of_accounts_templates</c> tablosu için EF Core map'i.
/// Catalog DB'de sistem geneli TDHP şablon verisi; tenant izolasyonu yoktur,
/// xmin token veya soft-delete filter eklenmez.
/// Code benzersiz unique index.
/// </summary>
internal sealed class ChartOfAccountsTemplateConfiguration : IEntityTypeConfiguration<ChartOfAccountsTemplate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChartOfAccountsTemplate> builder)
    {
        builder.ToTable("chart_of_accounts_templates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        // Şablon kodu sistem genelinde benzersiz
        builder.HasIndex(x => x.Code)
            .HasDatabaseName("ix_chart_of_accounts_templates_code")
            .IsUnique();

        builder.Property(x => x.Code)
            .HasMaxLength(12)
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(x => x.ParentCode).HasMaxLength(12);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.Property(x => x.Level).HasConversion<short>().IsRequired();
        builder.Property(x => x.AccountClass).HasConversion<short>().IsRequired();
        builder.Property(x => x.AccountType).HasConversion<short>().IsRequired();

        builder.Property(x => x.IsRequired).IsRequired();
        builder.Property(x => x.IsDetail).IsRequired();
        builder.Property(x => x.IsMonetary).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();

        // Audit (BaseEntity'den) — Catalog LookUp entity'si; soft-delete yoktur
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // NO xmin — Catalog LookUp, tenant-scoped değil
    }
}
