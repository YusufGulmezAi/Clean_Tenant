using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>account_codes</c> tablosu için EF Core map'i.
/// 3 kademeli TDHP hesap kodu hiyerarşisi, kod format CHECK + level uyum CHECK,
/// citext arama alanları ve xmin concurrency token.
/// </summary>
internal sealed class AccountCodeConfiguration : IEntityTypeConfiguration<AccountCode>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AccountCode> builder)
    {
        builder.ToTable("account_codes", t =>
        {
            // Kod formatı: Ana=3h, Yardımcı=6h (3-3), Detay=9h (3-3-3)
            t.HasCheckConstraint("ck_account_code_format",
                "code ~ '^[1-9][0-9]{2}(\\.[0-9]{3}(\\.[0-9]{3})?)?$'");
            // Kademe ile kod uzunluğu tutarlılığı (nokta hariç toplam hane)
            t.HasCheckConstraint("ck_account_code_level_match",
                "(level = 0 AND char_length(replace(code, '.', '')) = 3) OR " +
                "(level = 1 AND char_length(replace(code, '.', '')) = 6) OR " +
                "(level = 2 AND char_length(replace(code, '.', '')) = 9)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        // CompanyId + Code şirket içinde benzersiz (soft-delete hariç)
        builder.HasIndex(x => new { x.CompanyId, x.Code })
            .HasDatabaseName("ix_account_codes_company_code")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.Code)
            .HasMaxLength(12)
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(x => x.ParentCode).HasMaxLength(12);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.Level).HasConversion<short>().IsRequired();
        builder.Property(x => x.AccountClass).HasConversion<short>().IsRequired();
        builder.Property(x => x.AccountType).HasConversion<short>().IsRequired();
        builder.Property(x => x.Source).HasConversion<short>().IsRequired();

        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.IsDetail).IsRequired();
        builder.Property(x => x.IsMonetary).IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();

        builder.Property(x => x.TemplateCode).HasMaxLength(12);

        // Audit + soft delete (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin concurrency token
        builder.UseXminAsConcurrencyToken();
    }
}
