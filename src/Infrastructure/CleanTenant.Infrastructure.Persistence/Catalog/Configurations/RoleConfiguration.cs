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

        // v0.2.8.b — Tenant-spesifik roller için sahiplik kolonları.
        // null = global rol; dolu = sadece bu tenant/company'nin admin'i yönetir.
        builder.Property(r => r.TenantId);
        builder.Property(r => r.CompanyId);
        builder.HasIndex(r => r.TenantId).HasDatabaseName("ix_role_tenant_id");
        builder.HasIndex(r => r.CompanyId).HasDatabaseName("ix_role_company_id");

        builder.Property(r => r.IsBuiltIn).IsRequired();

        // Eski index'i ((NormalizedName, Scope) unique) drop ediliyor; yerine
        // (NormalizedName, Scope, TenantId, CompanyId) gelir. Böylece aynı
        // rol adı farklı tenant'larda bağımsız oluşturulabilir (örn. Tenant A
        // "MuhasebeYöneticisi" + Tenant B "MuhasebeYöneticisi" çakışmaz).
        // NULL == NULL davranışı PostgreSQL'de NULLS NOT DISTINCT klozu olmadan
        // farklı sayılır; bu yüzden globaller arasında (NormalizedName, Scope,
        // NULL, NULL) için EF migration NULLS NOT DISTINCT eklenmeli — manuel
        // migration ile yapacağız.
        builder.HasIndex(r => new { r.NormalizedName, r.Scope, r.TenantId, r.CompanyId })
            .IsUnique()
            .HasDatabaseName("ix_role_normalized_name_scope_tenant_company");

        builder.UseXminAsConcurrencyToken();

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
