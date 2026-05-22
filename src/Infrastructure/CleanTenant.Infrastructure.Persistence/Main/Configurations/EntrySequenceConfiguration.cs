using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>entry_sequences</c> tablosu için EF Core map'i.
/// Her şirket + mali yıl + fiş tipi kombinasyonu için tek sayaç satırı garantisi.
/// IAggregateRoot değil — xmin token eklenmez.
/// </summary>
internal sealed class EntrySequenceConfiguration : IEntityTypeConfiguration<EntrySequence>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EntrySequence> builder)
    {
        builder.ToTable("entry_sequences");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.FiscalYearId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.FiscalYearId);

        // CompanyId + FiscalYearId + EntryType kombinasyonu benzersiz — tek sayaç
        builder.HasIndex(x => new { x.CompanyId, x.FiscalYearId, x.EntryType })
            .HasDatabaseName("ix_entry_sequences_company_year_type")
            .IsUnique();

        builder.Property(x => x.EntryType).HasConversion<short>().IsRequired();
        builder.Property(x => x.LastNumber).IsRequired();

        // Audit (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // NOT UseXminAsConcurrencyToken — IAggregateRoot değil
    }
}
