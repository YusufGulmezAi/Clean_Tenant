using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>journal_lines</c> tablosu için EF Core map'i.
/// Borç/alacak işareti CHECK (ikisi birden sıfırdan büyük olamaz),
/// denormalize hesap kodu alan adı ve decimal hassasiyet tanımları.
/// IAggregateRoot değil — xmin token eklenmez.
/// </summary>
internal sealed class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.ToTable("journal_lines", t =>
        {
            // debit ve credit negatif olamaz; ikisi birden sıfırdan büyük olamaz
            t.HasCheckConstraint("ck_journal_line_sign",
                "debit >= 0 AND credit >= 0 AND (debit = 0 OR credit = 0)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.JournalEntryId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.JournalEntryId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.AccountCodeId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.AccountCodeId);

        // AccountCodeValue = denormalize hesap kodu string (FK ile aynı ada çakışmayı önler)
        builder.Property(x => x.AccountCodeValue)
            .HasColumnName("account_code_value")
            .HasMaxLength(12)
            .IsRequired();

        builder.Property(x => x.Debit).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Credit).HasPrecision(18, 4).IsRequired();

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.CostCenterId).HasColumnType("uuid");
        builder.HasIndex(x => x.CostCenterId).HasFilter("cost_center_id IS NOT NULL");

        builder.Property(x => x.ProjectId).HasColumnType("uuid");
        builder.Property(x => x.UnitId).HasColumnType("uuid");

        builder.Property(x => x.TaxCode).HasMaxLength(20);
        builder.Property(x => x.OriginalCurrency).HasMaxLength(10);
        builder.Property(x => x.OriginalAmount).HasPrecision(18, 4);
        builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);

        // Audit (BaseEntity'den) — soft delete yok; satır fişle birlikte cascade silinir
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // NOT UseXminAsConcurrencyToken — IAggregateRoot değil
    }
}
