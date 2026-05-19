using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Support;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Domain.Localization;
using CleanTenant.Domain.LookUp.Banks;
using CleanTenant.Domain.LookUp.BuildingTypes;
using CleanTenant.Domain.LookUp.Districts;
using CleanTenant.Domain.LookUp.Neighborhoods;
using CleanTenant.Domain.LookUp.Provinces;
using CleanTenant.Domain.LookUp.ResidentialTypes;
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

    /// <inheritdoc />
    public DbSet<Province> Provinces => Set<Province>();

    /// <inheritdoc />
    public DbSet<District> Districts => Set<District>();

    /// <inheritdoc />
    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();

    /// <inheritdoc />
    public DbSet<ResidentialType> ResidentialTypes => Set<ResidentialType>();

    /// <inheritdoc />
    public DbSet<BuildingType> BuildingTypes => Set<BuildingType>();

    /// <inheritdoc />
    public DbSet<Bank> Banks => Set<Bank>();

    /// <summary>v0.2.10 — Çok dilli string kaynakları.</summary>
    public DbSet<LocalizedResource> LocalizedResources => Set<LocalizedResource>();

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

        // Sadece Catalog/Configurations namespace'indeki IEntityTypeConfiguration<T>
        // implementasyonlarını yükle. Filtre olmadan tüm assembly taranır ve
        // Main/Audit DbContext'lerine ait configuration'lar da Catalog model'ine
        // sızar (v0.2.3.c'de tespit edildi).
        builder.ApplyConfigurationsFromAssembly(
            typeof(CatalogDbContext).Assembly,
            type => type.Namespace?.StartsWith(typeof(CatalogDbContext).Namespace + ".Configurations", StringComparison.Ordinal) == true);
    }
}
