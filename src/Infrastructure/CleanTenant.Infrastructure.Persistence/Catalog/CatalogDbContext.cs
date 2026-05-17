using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Support;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Infrastructure.Persistence.Identifiers;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Persistence.Catalog;

/// <summary>
/// <para>
/// Catalog veritabanına bağlanan EF Core DbContext. ASP.NET Core Identity'nin
/// <see cref="IdentityDbContext{TUser, TRole, TKey}"/>'inden miras alır;
/// Identity'nin standart kullanıcı/rol/claim/token/login tablolarını otomatik
/// yönetir. Üzerine CleanTenant'a özel tabloları ekler.
/// </para>
/// <para>
/// <b>Naming Convention:</b> <c>UseSnakeCaseNamingConvention()</c> ile tablo
/// ve sütun adları otomatik <c>snake_case</c>'e dönüştürülür (PostgreSQL
/// konvansiyonu).
/// </para>
/// <para>
/// <b>Application Soyutlaması:</b> <see cref="ICatalogDbContext"/>
/// arabirimini implement eder; Application katmanı concrete tip yerine
/// arabirimi tüketir.
/// </para>
/// </summary>
public sealed class CatalogDbContext : IdentityDbContext<User, Role, Guid>, ICatalogDbContext
{
    /// <summary>Standart EF Core ctor; DbContextOptions DI ile gelir.</summary>
    /// <param name="options">Bağlantı string'i ve provider konfigürasyonu.</param>
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <inheritdoc />
    public DbSet<TenantConnection> TenantConnections => Set<TenantConnection>();

    /// <inheritdoc />
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <inheritdoc />
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <inheritdoc />
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    /// <inheritdoc />
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <inheritdoc />
    public DbSet<SupportSession> SupportSessions => Set<SupportSession>();

    /// <summary>
    /// URL kod havuzu (Infrastructure-only; <see cref="ICatalogDbContext"/>
    /// üzerinden expose edilmez — yalnız UrlCodeGeneratingInterceptor v0.1.4.b'de erişir).
    /// </summary>
    public DbSet<UrlCodeRegistry> UrlCodeRegistry => Set<UrlCodeRegistry>();

    /// <summary>
    /// IdentityDbContext'in <see cref="DbContext.OnModelCreating"/> override'ını
    /// çağırır, ardından kendi Configuration sınıflarımızı uygular.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tüm IEntityTypeConfiguration<T> implementasyonları aynı assembly'den
        // otomatik yüklenir; Configurations klasöründeki dosyalar buraya akar.
        builder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
