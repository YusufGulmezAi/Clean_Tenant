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
