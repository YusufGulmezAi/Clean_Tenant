using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>accounting_bank_accounts</c> tablosu için EF Core map'i.
/// LookUp katmanındaki <c>bank_accounts</c> tablosundan farklı; bu tablo
/// muhasebe modülüne ait şirket banka hesaplarını saklar.
/// TR IBAN format CHECK ve xmin concurrency token.
/// </summary>
internal sealed class AccountingBankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("accounting_bank_accounts", t =>
        {
            // Türkiye IBAN formatı: TR + 24 rakam (toplam 26 karakter)
            t.HasCheckConstraint("ck_bank_iban_format",
                "iban IS NULL OR iban ~ '^TR[0-9]{24}$'");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BankName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BranchCode).HasMaxLength(20);
        builder.Property(x => x.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Iban).HasMaxLength(26);
        builder.Property(x => x.AccountType).HasConversion<short>().IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();

        // Hesap planı bağlantısı (opsiyonel — 102.xx grubu)
        builder.Property(x => x.AccountCodeId).HasColumnType("uuid");
        builder.HasIndex(x => x.AccountCodeId).HasFilter("account_code_id IS NOT NULL");

        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Audit + soft delete (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin concurrency token
        builder.UseXminAsConcurrencyToken();
    }
}
