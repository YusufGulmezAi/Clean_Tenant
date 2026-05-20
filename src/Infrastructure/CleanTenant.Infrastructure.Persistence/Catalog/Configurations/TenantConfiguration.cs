using CleanTenant.Domain.Identity.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <see cref="Tenant"/> entity'sinin EF Core eşlemesi. PK, UrlCode unique
/// index, citext kolon tipi (Name için case-insensitive arama), xmin
/// concurrency token ve audit alanları konfigüre edilir.
/// </summary>
public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.UrlCode)
            .IsRequired()
            .HasMaxLength(9)
            .IsFixedLength();
        builder.HasIndex(t => t.UrlCode).IsUnique();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnType("citext");
        builder.HasIndex(t => t.Name).IsUnique();

        builder.Property(t => t.LegalName)
            .HasMaxLength(512);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(t => t.BillingTier)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(t => t.DatabaseSchemaName)
            .HasMaxLength(63);

        // v0.2.3.c — Support Mode v2: Sistem kullanıcısının bu Yönetim'de
        // write erişimine izin var mı? Default true; YönetimAdmin parametreyi
        // mail link onayıyla kapatabilir.
        builder.Property(t => t.AllowSystemWriteAccess)
            .IsRequired()
            .HasDefaultValue(true);

        // v0.2.4.b — Yasal kimlik (VKN / TCKN / YKN — mutually exclusive)
        builder.Property(t => t.LegalIdentityType)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(t => t.LegalIdentityNumber)
            .IsRequired()
            .HasMaxLength(11);

        // Global tekil — herhangi bir Yönetim aynı kimlikle kayıt edilemez.
        builder.HasIndex(t => t.LegalIdentityNumber).IsUnique();

        // Format CHECK constraint: tipe göre regex
        builder.ToTable(tb => tb.HasCheckConstraint(
            "ck_tenant_legal_identity_format",
            "(legal_identity_type = 1 AND legal_identity_number ~ '^[0-9]{10}$') OR " +
            "(legal_identity_type = 2 AND legal_identity_number ~ '^[1-9][0-9]{10}$') OR " +
            "(legal_identity_type = 3 AND legal_identity_number ~ '^99[0-9]{9}$')"));

        builder.Property(t => t.Address)
            .HasMaxLength(512);

        // v0.2.11.b — Adres FK'ları (LookUp), İletişim ve Sözleşme alanları
        builder.Property(t => t.ProvinceId);
        builder.Property(t => t.DistrictId);
        builder.Property(t => t.NeighborhoodId);
        builder.HasIndex(t => t.ProvinceId);
        builder.HasIndex(t => t.DistrictId);
        builder.HasIndex(t => t.NeighborhoodId);

        builder.Property(t => t.ContactPerson).HasMaxLength(200);
        builder.Property(t => t.ContactEmail).HasMaxLength(256);
        builder.Property(t => t.ContactPhone).HasMaxLength(32);

        builder.Property(t => t.ContractStartDate);
        builder.Property(t => t.ContractEndDate);
        builder.Property(t => t.TransitionGraceDays);

        // xmin → RowVersion (PostgreSQL optimistic concurrency)
        builder.UseXminAsConcurrencyToken();

        // Audit alanları
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt);
        builder.Property(t => t.DeletedAt);

        // Soft delete query filter — yalnız silinmemiş tenant'lar default sorgularda görünür.
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
