using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>invoices</c> tablosu için EF Core map'i.
/// Tutar tutarlılığı CHECK (SubTotal + VatAmount = TotalAmount),
/// CompanyId + InvoiceNumber + Direction benzersizliği ve xmin concurrency token.
/// </summary>
internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", t =>
        {
            // Negatif tutar yasak; KDV dahil toplam = KDV hariç + KDV
            t.HasCheckConstraint("ck_invoice_amounts",
                "sub_total >= 0 AND vat_amount >= 0 AND total_amount = sub_total + vat_amount");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.AccountingPeriodId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.AccountingPeriodId);

        // Aynı şirket içinde aynı fatura numarası ve yönü benzersiz (soft-delete hariç)
        builder.HasIndex(x => new { x.CompanyId, x.InvoiceNumber, x.Direction })
            .HasDatabaseName("ix_invoices_company_number_direction")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Direction).HasConversion<short>().IsRequired();
        builder.Property(x => x.CounterpartyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CounterpartyTaxId).HasMaxLength(20);

        builder.Property(x => x.AccountCodeId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.AccountCodeId);

        builder.Property(x => x.SubTotal).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.VatCategory).HasConversion<short>().IsRequired();
        builder.Property(x => x.VatAmount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 4).IsRequired();

        builder.Property(x => x.IsPostedToJournal).HasDefaultValue(false);
        builder.Property(x => x.JournalEntryId).HasColumnType("uuid");
        builder.HasIndex(x => x.JournalEntryId).HasFilter("journal_entry_id IS NOT NULL");

        builder.Property(x => x.Notes).HasMaxLength(1000);

        // Audit + soft delete (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin concurrency token
        builder.UseXminAsConcurrencyToken();
    }
}
