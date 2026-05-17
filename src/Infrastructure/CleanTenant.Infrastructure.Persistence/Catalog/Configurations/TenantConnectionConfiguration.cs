using CleanTenant.Domain.Identity.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <see cref="TenantConnection"/> entity'sinin EF Core eşlemesi.
/// Tenant ile bire-çok ilişki (rotation sırasında geçiş penceresi için
/// birden çok satır olabilir; her zaman bir aktif).
/// </summary>
public sealed class TenantConnectionConfiguration : IEntityTypeConfiguration<TenantConnection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantConnection> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TenantId).IsRequired();
        builder.HasIndex(c => c.TenantId);

        builder.Property(c => c.ConnectionStringEncrypted)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(c => c.IsActive).IsRequired();

        // Yalnız her tenant için tek aktif bağlantı bulunabilir (partial unique index).
        builder.HasIndex(c => new { c.TenantId, c.IsActive })
            .IsUnique()
            .HasFilter("is_active = TRUE");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.UseXminAsConcurrencyToken();
    }
}
