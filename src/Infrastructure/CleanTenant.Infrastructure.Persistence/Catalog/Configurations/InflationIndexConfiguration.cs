using CleanTenant.Domain.LookUp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <c>inflation_indexes</c> tablosu için EF Core map'i.
/// Catalog DB'de tüm tenant'ların paylaştığı TÜİK ÜFE/TÜFE verileri.
/// Ay aralığı CHECK (1–12), pozitif endeks CHECK; Year + Month benzersiz.
/// Tenant izolasyonu yoktur; xmin token eklenmez.
/// </summary>
internal sealed class InflationIndexConfiguration : IEntityTypeConfiguration<InflationIndex>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InflationIndex> builder)
    {
        builder.ToTable("inflation_indexes", t =>
        {
            t.HasCheckConstraint("ck_inflation_month",
                "month BETWEEN 1 AND 12");
            t.HasCheckConstraint("ck_inflation_index",
                "index_value > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        // Year + Month kombinasyonu benzersiz — aylık tek endeks değeri
        builder.HasIndex(x => new { x.Year, x.Month })
            .HasDatabaseName("ix_inflation_indexes_year_month")
            .IsUnique();

        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.Month).IsRequired();
        builder.Property(x => x.IndexValue).HasPrecision(18, 6).IsRequired();

        // Audit (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // NO xmin — Catalog LookUp, tenant-scoped değil
    }
}
