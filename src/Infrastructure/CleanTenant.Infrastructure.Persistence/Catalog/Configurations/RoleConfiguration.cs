using CleanTenant.Domain.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <see cref="Role"/> entity'sinin EF Core eşlemesi.
/// IdentityDbContext'in Role konfigürasyonunu ek alanlarımızla
/// (UrlCode, Scope, Description, IsBuiltIn, audit, soft delete, xmin)
/// genişletir. Unique index <c>(NormalizedName, Scope)</c> bileşik —
/// aynı isim farklı scope'larda olabilir.
/// </summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(r => r.UrlCode)
            .IsRequired()
            .HasMaxLength(9)
            .IsFixedLength();
        builder.HasIndex(r => r.UrlCode).IsUnique();

        builder.Property(r => r.Name).HasColumnType("citext");
        builder.Property(r => r.NormalizedName).HasColumnType("citext");

        builder.Property(r => r.Description).HasMaxLength(512);

        builder.Property(r => r.Scope)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(r => r.IsBuiltIn).IsRequired();

        // Identity'nin default unique index'i sadece NormalizedName üzerine.
        // Bizim ihtiyacımız (NormalizedName, Scope) bileşik unique.
        // Default index'i yeniden tanımlayamıyoruz; ek bileşik index ekliyoruz.
        builder.HasIndex(r => new { r.NormalizedName, r.Scope })
            .IsUnique()
            .HasDatabaseName("ix_role_normalized_name_scope");

        builder.UseXminAsConcurrencyToken();

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
