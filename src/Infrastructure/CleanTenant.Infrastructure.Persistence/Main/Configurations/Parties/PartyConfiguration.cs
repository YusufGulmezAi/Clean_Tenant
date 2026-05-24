using CleanTenant.Domain.Tenant.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Parties;

/// <summary>
/// <c>parties</c> tablosu — cari kişi (malik/kiracı/iletişim). UrlCode unique;
/// (company_id, tckn) kısmi unique; xmin concurrency.
/// </summary>
internal sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        builder.ToTable("parties");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.UrlCode).HasMaxLength(9).IsRequired();
        builder.HasIndex(x => x.UrlCode).IsUnique();

        builder.Property(x => x.Kind).HasConversion<short>().IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.TradeName).HasMaxLength(200);
        builder.Property(x => x.Tckn).HasMaxLength(11);
        builder.Property(x => x.Vkn).HasMaxLength(10);
        builder.Property(x => x.BirthDate);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.AddressLine).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.TagsJson).HasColumnType("jsonb");
        builder.Property(x => x.KvkkConsentGiven).HasDefaultValue(false);
        builder.Property(x => x.KvkkConsentAt).HasColumnType("timestamptz");
        builder.Property(x => x.KvkkConsentChannel).HasMaxLength(50);
        builder.Property(x => x.LinkedUserId).HasColumnType("uuid");

        // Aynı şirkette aynı TCKN tek Party (kısmi unique; eksik TCKN'yi kapsamaz)
        builder.HasIndex(x => new { x.CompanyId, x.Tckn })
            .HasDatabaseName("ix_parties_company_tckn")
            .HasFilter("tckn IS NOT NULL AND is_deleted = false")
            .IsUnique();

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.UseXminAsConcurrencyToken();
    }
}
