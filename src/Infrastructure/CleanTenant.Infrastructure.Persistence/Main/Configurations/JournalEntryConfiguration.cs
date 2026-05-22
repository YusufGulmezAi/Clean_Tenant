using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>journal_entries</c> tablosu için EF Core map'i.
/// Posted durumunda borç-alacak eşitliği CHECK, fiş satırları Cascade,
/// tutarlar decimal(18,4) ve xmin concurrency token.
/// </summary>
internal sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries", t =>
        {
            // status=2 → Posted; yalnızca muhasebeleştirilmiş fişlerde borç-alacak eşitliği zorunlu
            t.HasCheckConstraint("ck_journal_balanced",
                "status != 2 OR total_debit = total_credit");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.AccountingPeriodId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.AccountingPeriodId);

        // CompanyId + EntryNumber şirket genelinde benzersiz
        builder.HasIndex(x => new { x.CompanyId, x.EntryNumber })
            .HasDatabaseName("ix_journal_entries_company_number")
            .IsUnique();

        builder.Property(x => x.EntryType).HasConversion<short>().IsRequired();
        builder.Property(x => x.EntryNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(200);
        builder.Property(x => x.ReferenceId).HasColumnType("uuid");

        builder.Property(x => x.TotalDebit).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.TotalCredit).HasPrecision(18, 4).IsRequired();

        builder.Property(x => x.Status).HasConversion<short>().IsRequired();

        builder.Property(x => x.PostedAt).HasColumnType("timestamptz");
        builder.Property(x => x.PostedBy).HasColumnType("uuid");
        builder.Property(x => x.ApprovedAt).HasColumnType("timestamptz");
        builder.Property(x => x.ApprovedBy).HasColumnType("uuid");
        builder.Property(x => x.VoidedAt).HasColumnType("timestamptz");
        builder.Property(x => x.VoidedBy).HasColumnType("uuid");
        builder.Property(x => x.VoidReason).HasMaxLength(500);
        builder.Property(x => x.OriginalEntryId).HasColumnType("uuid");

        // e-Defter XML alanı — büyük metin, max length sınırı yok
        builder.Property(x => x.EDefterXml).HasColumnType("text");

        // Fiş satırları ilişkisi — Cascade: fiş silinince satırlar da silinir
        builder.HasMany(e => e.Lines)
            .WithOne(l => l.JournalEntry)
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit + soft delete (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin concurrency token
        builder.UseXminAsConcurrencyToken();
    }
}
